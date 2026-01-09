# 项目介绍
这是一个通过C#编写的自动测试项目。
1. 使用wpf + prism + mvvm架构
2. 数据库使用sqlite，用sugarorm来操作数据库。
3. 项目每个模块的功能为
    1. FluorescenceFullAutomatic.Core 模块：包含项目的Model的定义，比如状态类，枚举类等在每个模块都会用的类。
    2. Platform 模块：包含项目平台相关的代码，比如自定义的IValueConverter，自定义的通讯类，通用的工具类，通用的图片资源，数据库相关，日志相关，style,通用的viewmodel,比如提示框。
    3. HomeModule 模块：包含项目的主界面相关的代码，比如主界面的view,viewmodel,以及主界面的导航栏。
    4. UploadModule 模块：包含项目的上传界面相关的代码，比如上传界面的view,viewmodel,以及上传界面的导航栏。
4. 项目MVVM的具体实现为,已HomeViewModel为例。
    1. 每个页面（窗口和用户控件）都有一个对应的viewmodel，viewmodel负责处理页面的业务逻辑，比如数据绑定，事件处理等。
    2. HomeViewModel的构造函数通过依赖注入的方式，将HomeModule模块的需要的服务注入到HomeViewModel中。
    3. HomeViewModel中除了注入一些 日志，线程调度，工具类的Service外，还有一个IHomeService，IHomeService负责处理HomeViewModel的基本业务逻辑，所以在IHomeService中还会注入一些Service（比如数据库服务，通讯服务等）来调用Service的方法。

# HomeViewModel 主检测业务流程（只含核心业务步骤）

范围说明：
1. 只描述 HomeViewModel 的“主检测/取样/检测”业务步骤与状态流转。
2. 不展开分析日志、数据库、弹框、线程调度等工具性 Service 的实现细节。
3. 关键业务入口与回调均来自：HomeModule\ViewModels\HomeViewModel.cs

## 业务状态（MachineStatus）

- 等待自检：None
- 已就绪：SelfInspectionSuccess / TestingEnd
- 检测中（取样阶段）：Sampling
- 取样完成：SamplingFinished
- 检测中（仅等待反应区检测任务）：Testing
- 自检失败：SelfInspectionFailed
- 运行错误：RunningError

对应定义：FluorescenceFullAutomatic.Core\Config\MachineStatus.cs

## 总体流程概览（从“点击开始检测”开始）

1. 点击开始检测：ClickStart()
2. 状态校验：VerifyMachineState()
   - 未自检/自检失败/反应区已满/运行错误/正在检测中 → 弹提示并中止
3. 初始化本轮检测状态：InitState()
   - 设置 MachineStatus=Sampling，清空样本架/当前样本索引/各种命令完成标志位
4. 获取仪器状态：GetMachineState()
   - 首次获取状态回调：ReceiveMachineStatusModel()
   - 校验“卡仓存在 + 卡数>0 + 清洗液存在 + 样本架存在”
     - 不满足：提示“重新获取/结束检测”
     - 满足：InitSampleShelfState() 并进行首次清洗取样针 CleanoutSamplingProbe()
5. 首次清洗完成：ReceiveCleanoutSamplingProbeModel()
   - 触发 MoveSampleShelfFirst() → 移动到第一排存在的样本架
6. 移动样本架完成：ReceiveMoveSampleShelfModel()
   - InitCurrentSampleShelfState() → 当前排样本索引复位
   - MoveSampleNext() → 开始遍历这一排的 0..4 号样本位
7. 移动到某个样本位：MoveSample(pos) → ReceiveMoveSampleModel()
   - 根据样本类型分支（无样本/样本管/样本杯）
8. 对“存在的样本”执行：扫码（可选）→ 取样 → 推卡 → 加样 → 入反应区队列
9. 反应区到时出队触发检测：OnReactionAreaDequeue() → Test() → ReceiveTestModel()
10. 继续下一样本/下一排样本架，直到“取样结束”，并在需要时等待反应区检测完成

## 详细业务流转（核心步骤与分支）

### A. 自检流程（启动/手动自检）

- 触发点
  - Loaded() 首次加载：GoGetSelfMachineStatus()
  - 点击自检：ClickSelfMachineStatus() → GoGetSelfMachineStatus()
- 执行自检：GetSelfInspectionState()
- 自检回调：ReceiveGetSelfMachineStatusModel()
  - 无错误码：MachineStatus=SelfInspectionSuccess → GetMachineState()（刷新卡仓/清洗液等状态）
  - 有错误码：MachineStatus=SelfInspectionFailed（提供“重新自检/暂不自检”）

### B. 主检测取样主循环（样本架→样本→扫码→取样→推卡→加样）

1. 移动到样本架：MoveSampleShelf(pos) → ReceiveMoveSampleShelfModel()
2. 在该排内移动样本位：MoveSampleNext() → MoveSample(pos) → ReceiveMoveSampleModel()
3. ReceiveMoveSampleModel() 的三类分支：
   - 样本不存在（SampleType=None）
     - 标记该位置不存在
     - 如果已是最后一排最后一个：触发“取样结束”
     - 否则：MoveSampleNext() 继续下一个样本位
   - 样本管（SampleTube）
     - 生成本次检测结果记录（TestResult）
     - 如果配置要求扫码：ScanBarcode()
       - 扫码成功回调：OnScanSuccess() → ScanSuccess(barcode)
         - 记录条码并进入“取样+推卡”
       - 扫码失败回调：OnScanFailed() → ScanFailed()
         - 跳过该样本，MoveSampleNext()
     - 如果不需要扫码：直接进入“取样+推卡”
   - 样本杯（SampleCup）
     - 生成本次检测结果记录（TestResult）
     - 不扫码，直接进入“取样+推卡”

4. 取样 + 推卡并行触发：VerifyStateSamplingPushCard(sampleType)
   - 取样：Sampling(type, volume)
     - 若此刻取样针仍在清洗中：记录“待恢复取样”，等清洗完成后自动补发取样
   - 推卡：PushCardGetMachineState() → ReceiveMachineStatusModel()（推卡专用分支）→ PushCard()
     - 没有卡/卡仓不存在：提示“已添加/重新检查”后继续重试获取状态

5. 推卡回调：ReceivePushCardModel()
   - 推卡成功且有二维码：解析检测卡项目 → GoAddingSample()
   - 推卡失败：最多重试 3 次（超过则触发“结束检测：推卡错误”）

6. 满足“加样条件”才真正发出加样命令：GoAddingSample()
   - 条件：推卡完成 + 取样完成 + 推卡成功 + 取样针清洗完成 + 非“等待恢复推卡”
   - 执行：AddingSample(volume, "1")

7. 加样回调：ReceiveAddingSampleModel()
   - 移动检测卡到反应区：GoMoveReactionArea() → MoveReactionArea(x,y)
   - 若已取到最后一个样本：触发“取样结束”
   - 若反应区满（队列达到阈值）：暂不继续取样，等待检测释放位置
   - 否则：清洗取样针 → 获取仪器状态后继续下一样本（MoveSampleNextGetMachineState()）

### C. 反应区孵育与检测（入队→到时出队→检测→结果）

1. 移动反应区完成：ReceiveMoveReactionAreaModel()
   - 将该卡位标记为“等待”
   - 加入待检测队列：Enqueue(item)
2. 到孵育时间出队：OnReactionAreaDequeue(item)
   - 若当前不在检测中：发出 Test(x,y,...) 并进入 IsTesting=true
3. 检测完成回调：ReceiveTestModel()
   - 更新检测结果与反应区状态为“结束”
   - 若队列为空：
     - 若已取样完成（SamplingFinished）或处于 Testing/RunningError：MachineStatus=TestingEnd
     - 否则：说明仍在取样中，仅记录“检测完成但还有待取样样本”
   - 若之前因反应区满而暂停取样：恢复 MoveSampleNextGetMachineState()

### D. 取样结束收尾（清洗取样针 + 样本架复位）

- 触发点
  - 全部样本处理完毕
  - 推卡连续失败超过上限
  - 用户选择“结束检测”
- 执行：TestFinishedAction()
  1. MachineStatus=SamplingFinished
  2. 清洗取样针（若未在清洗/逻辑允许）
  3. 样本架复位：MoveSampleShelf(-1)
- 两个动作都完成后：ShowFinishedDialog()
  - 若待测队列为空：MachineStatus=TestingEnd
  - 否则：MachineStatus=Testing（仍有卡在反应区等待检测）

## 流程图（Mermaid）

```mermaid
flowchart TD
    A[点击开始检测 ClickStart] --> B{VerifyMachineState 通过?}
    B -- 否 --> Z[提示并中止]
    B -- 是 --> C[InitState<br/>MachineStatus=Sampling]
    C --> D[GetMachineState]
    D --> E{卡仓存在?<br/>卡数>0?<br/>清洗液存在?<br/>样本架存在?}
    E -- 否 --> Z2[提示:重新获取/结束检测]
    E -- 是 --> F[InitSampleShelfState]
    F --> G[首次清洗取样针 CleanoutSamplingProbe]
    G --> H[MoveSampleShelfFirst]
    H --> I[ReceiveMoveSampleShelfModel<br/>InitCurrentSampleShelfState]
    I --> J[MoveSampleNext -> MoveSample(pos)]
    J --> K{样本类型}
    K -- 不存在 --> J2{最后一排最后一个?}
    J2 -- 是 --> END[取样结束 TestFinishedAction]
    J2 -- 否 --> J
    K -- 样本杯 --> P[VerifyStateSamplingPushCard]
    K -- 样本管 --> L{需要扫码?}
    L -- 是 --> M[ScanBarcode]
    M -- 成功 --> P
    M -- 失败 --> J
    L -- 否 --> P

    P --> SAMP[Sampling 取样]
    P --> PCS[PushCardGetMachineState]
    PCS --> MS2[GetMachineState(推卡专用)]
    MS2 --> PC{有卡?}
    PC -- 否 --> PCS
    PC -- 是 --> PUSHCARD[PushCard 推卡]
    PUSHCARD --> PCOK{推卡成功且有二维码?}
    PCOK -- 否(<=3次) --> PCS
    PCOK -- 否(>3次) --> END
    PCOK -- 是 --> ADDCHK[GoAddingSample<br/>等待:取样+推卡+清洗完成]
    SAMP --> ADDCHK
    ADDCHK --> ADD[AddingSample 加样]
    ADD --> MRA[MoveReactionArea 移入反应区]
    MRA --> ENQ[Enqueue 入待测队列]

    ENQ --> CONT{取样结束或反应区满?}
    CONT -- 取样结束 --> END
    CONT -- 反应区满 --> WAIT[等待检测释放位置]
    CONT -- 继续取样 --> NEXT[清洗取样针 -> 获取状态]
    NEXT --> J

    ENQ --> DQ[孵育到时出队 OnReactionAreaDequeue]
    DQ --> TEST[Test 检测]
    TEST --> RES[ReceiveTestModel 更新结果]
    RES --> WAIT
```

## 检测流程重构建议（策略 + 状态机）

重构目标：
1. 将“流程控制”从 HomeViewModel 抽离为可测试的业务引擎，VM 只做展示与事件转发
2. 用状态机管理宏观阶段，避免大量 bool 标志位与回调顺序耦合
3. 用策略封装可变规则（是否扫码/样本类型/推卡重试/反应区满载策略等），减少 if/else 堆叠
4. 将串口“发命令+等回调”封装为可 await 的异步命令，提升可读性与可控性

推荐重构顺序（按低风险、可持续交付）：

阶段 1：命令 Task 化（Command Facade）
- 目标：将 serialPortService 的“发命令+等待 ReceiveXxx 回调”收敛为 Task 返回值
- 过程：
  - 为每个硬件命令提供对应的 Async 方法（例如 PushCardAsync/MoveSampleAsync/SamplingAsync 等）
  - 内部使用 TaskCompletionSource，把 ReceivePushCardModel/ReceiveMoveSampleModel 等回调转换成 Task 完成
  - 保持现有 HomeViewModel 的业务逻辑不变，只替换调用路径，确保行为一致
- 验收标准：
  - 任意一个命令路径可用 await 串联完成
  - 回调只在一个集中位置完成 Task，不在多处散落拼状态
  - 需要生成测试类来保证可测试，要适配现在的可测试类 FakeSerialPortImpl

阶段 2：引入单线程事件循环（Mailbox）
- 目标：消除并发回调导致的竞态与顺序不确定，把所有“事件处理”串行化
- 过程：
  - 定义事件类型：Start/Stop/SelfInspectionCompleted/MachineStatusReceived/SampleMoved/PushCardCompleted 等
  - 所有外部输入（UI 点击、串口回调）只投递事件，不直接改业务状态
  - Mailbox 逐条处理事件，确保状态变更与动作发出按序发生
- 验收标准：
  - 关键路径（开始检测→取样→推卡→加样）无 “restoreXxx/Finished 组合条件” 的竞态问题

阶段 3：状态机骨架（先搬生命周期最外层）
- 目标：用状态机表达“检测生命周期”的宏观阶段与合法迁移
- 过程：
  - 定义核心状态：Idle/Ready/SelfInspecting/Sampling/Finishing/WaitingTest/Testing/Paused(Full)/Error
  - 先迁移最外层：Start→校验→首次 GetMachineState→首次清洗→移动到首个样本架
  - 每个状态只处理自己关心的事件，并输出下一步要执行的命令（Action/Command）
- 验收标准：
  - 外层流程不依赖 HomeViewModel 的字段组合判断即可推进
  - 任意非法事件在当前状态下可被忽略或转入 Error/提示路径

阶段 4：引入策略（按“可变规则”拆分）
- 目标：把阶段内的分支逻辑（大量 if/else）挪到策略实现中
- 建议最先拆的策略（收益最大）：
  - 扫码策略：是否扫码、扫码失败处理（跳过/重试/标记）
  - 样本策略：无样本/样本管/样本杯对应动作序列
  - 推卡策略：重试次数、每次失败是否先 GetMachineState、等待反应区移动完成再推卡
  - 反应区策略：分配下一位置、满载时暂停取样并在检测释放后恢复
- 验收标准：
  - ReceiveMoveSampleModel/ReceivePushCardModel 等“业务分支”明显变薄
  - 新增规则不需要改状态机/VM 主流程，只需要新增策略实现或替换策略

阶段 5：收敛 HomeViewModel（变成 Presenter）
- 目标：HomeViewModel 不再承担流程控制，减少可变状态与复杂度
- 过程：
  - VM 只负责：绑定显示（MachineStatus/StateMsg/样本架/反应区）、发起 Start/Stop、自检按钮事件
  - VM 只订阅引擎输出的状态快照（State Snapshot），并更新 UI
- 验收标准：
  - HomeViewModel 中不再包含“主检测流程推进”的核心分支逻辑
  - 业务流程可在不启动 UI 的情况下通过单元测试跑通

阶段 6：回归与验证（逐段替换、逐段删旧逻辑）
- 目标：每完成一段迁移，就删除对应旧的标志位与恢复变量，避免双轨逻辑
- 过程：
  - 每迁移一条链路（例如“样本管：扫码→取样→推卡→加样”）就删掉该链路相关的 Finished/restore 字段与判断
  - 用真实设备/模拟串口回放验证关键场景：扫码失败、反应区满、推卡失败重试、取样结束后仍有待测卡等
- 验收标准：
  - 旧字段数量随迁移单调下降
  - 同一业务场景只有一条实现路径（不再“新旧并存”）
