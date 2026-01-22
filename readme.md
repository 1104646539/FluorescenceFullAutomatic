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

# 检测流程架构（状态机设计）

## 架构概述

项目采用 **状态机 + 事件邮箱** 架构：

```
┌─────────────────────────────────────────────────────────────────┐
│                     HomeViewModel                               │
│  - UI 事件转发（点击开始/自检）                                 │
│  - 接收状态机事件通知，更新 UI                                  │
│  - 检测操作调度（反应区队列时间驱动）                           │
└─────────────────────────────────┬───────────────────────────────┘
                                  │ 事件邮箱
                                  ▼
┌─────────────────────────────────────────────────────────────────┐
│                 DetectionStateMachine                           │
│  - 取样流程状态机（Stateless 库）                               │
│  - 硬件指令下发与回调处理                                       │
│  - 通过事件通知 VM 更新 UI                                      │
│  - 检测操作（独立于状态流转，只检查仪器异常）                   │
└─────────────────────────────────────────────────────────────────┘
```

## 业务状态（MachineStatus）

- 等待自检：None
- 已就绪：SelfInspectionSuccess / TestingEnd
- 检测中（取样阶段）：Sampling
- 取样完成：SamplingFinished
- 检测中（仅等待反应区检测任务）：Testing
- 自检失败：SelfInspectionFailed
- 运行错误：RunningError

对应定义：FluorescenceFullAutomatic.Core\Config\MachineStatus.cs

## 状态机状态（DetectionState）

| 状态 | 说明 |
|------|------|
| Idle | 空闲状态 |
| SelfInspecting | 自检中 |
| Ready | 就绪（自检成功） |
| PreparingDetection | 准备检测（获取仪器状态） |
| WaitingForFirstClean | 等待首次清洗取样针 |
| MovingToShelf | 移动到样本架 |
| PositioningAtSample | 定位样本位 |
| Scanning | 扫码 |
| SamplingAndPushingCard | 取样+推卡并行 |
| WaitingForCard | 等待添加检测卡 |
| AddingSample | 加样 |
| MovingToReactionArea | 移动到反应区 |
| PausedDueToFullQueue | 暂停（反应区满） |
| Finishing | 取样结束收尾 |
| WaitingForTests | 等待反应区检测完成 |
| Completed | 全部完成 |
| Error | 错误状态 |

对应定义：Platform\StateMachine\DetectionState.cs

## 状态机流转图

```mermaid
stateDiagram-v2
    [*] --> Idle
    
    %% 自检流程
    Idle --> SelfInspecting: RequestSelfInspection
    SelfInspecting --> Ready: SelfInspectionCompleted
    SelfInspecting --> Idle: SelfInspectionFailed
    Ready --> SelfInspecting: RequestSelfInspection
    
    %% 开始检测
    Ready --> PreparingDetection: StartDetection
    Completed --> PreparingDetection: StartDetection
    Completed --> SelfInspecting: RequestSelfInspection
    
    %% 准备阶段
    PreparingDetection --> WaitingForFirstClean: MachineStatusReceived
    PreparingDetection --> Completed: CancelDetection
    WaitingForFirstClean --> MovingToShelf: FirstCleanCompleted
    
    %% 取样循环
    MovingToShelf --> PositioningAtSample: ShelfMoveCompleted
    PositioningAtSample --> Scanning: SampleFound
    PositioningAtSample --> SamplingAndPushingCard: DirectSampling
    PositioningAtSample --> PositioningAtSample: MoveToNextSample
    PositioningAtSample --> MovingToShelf: MoveToNextShelf
    PositioningAtSample --> Finishing: NoMoreSamples
    PositioningAtSample --> Error: ErrorOccurred
    
    %% 扫码分支
    Scanning --> SamplingAndPushingCard: ScanSuccess
    Scanning --> PositioningAtSample: MoveToNextSample
    Scanning --> MovingToShelf: MoveToNextShelf
    Scanning --> Finishing: NoMoreSamples
    
    %% 取样推卡
    SamplingAndPushingCard --> AddingSample: AllConditionsMet
    SamplingAndPushingCard --> Finishing: PushCardFailed
    SamplingAndPushingCard --> WaitingForCard: NoCardAvailable
    SamplingAndPushingCard --> SamplingAndPushingCard: CleaningCompletedForSampling
    WaitingForCard --> SamplingAndPushingCard: CardAvailable
    
    %% 加样和反应区
    AddingSample --> MovingToReactionArea: AddingSampleCompleted
    MovingToReactionArea --> PausedDueToFullQueue: ReactionAreaFull
    MovingToReactionArea --> PositioningAtSample: MoveToNextSample
    MovingToReactionArea --> MovingToShelf: MoveToNextShelf
    MovingToReactionArea --> Finishing: NoMoreSamples
    PausedDueToFullQueue --> PositioningAtSample: ReactionAreaSpaceAvailable
    
    %% 结束流程
    Finishing --> Completed: TestQueueEmpty
    Finishing --> WaitingForTests: TestQueueNotEmpty
    WaitingForTests --> Completed: TestQueueEmpty
    
    %% 错误处理
    Error --> PreparingDetection: Retry
    Error --> Completed: ForceComplete
```

## 业务流程图

```mermaid
flowchart TD
    subgraph "自检流程"
        A1[点击自检/首次加载] --> A2[执行自检指令]
        A2 --> A3{自检结果}
        A3 -- 成功 --> A4[Ready 状态]
        A3 -- 失败 --> A5[提示重新自检]
    end
    
    subgraph "开始检测"
        B1[点击开始检测] --> B2{状态校验}
        B2 -- 失败 --> B3[提示并中止]
        B2 -- 通过 --> B4[获取仪器状态]
        B4 --> B5{状态校验<br/>卡仓/清洗液/样本架}
        B5 -- 失败 --> B6[提示重新获取]
        B5 -- 通过 --> B7[首次清洗取样针]
        B7 --> B8[移动到样本架]
    end
    
    subgraph "取样循环"
        C1[定位样本位] --> C2{样本类型}
        C2 -- 不存在 --> C3{最后一排最后一个?}
        C3 -- 是 --> END[取样结束]
        C3 -- 否 --> C1
        C2 -- 样本管 --> C4{需要扫码?}
        C4 -- 是 --> C5[扫码]
        C5 -- 成功 --> C6[取样+推卡]
        C5 -- 失败 --> C1
        C4 -- 否 --> C6
        C2 -- 样本杯 --> C6
        
        C6 --> C7{检测卡充足?}
        C7 -- 否 --> C8[等待添加卡]
        C8 --> C6
        C7 -- 是 --> C9{清洗完成?}
        C9 -- 否 --> C10[等待清洗]
        C10 --> C9
        C9 -- 是 --> C11[并行执行取样+推卡]
        
        C11 --> C12{取样完成 AND 推卡成功?}
        C12 -- 否 --> C13{推卡失败?}
        C13 -- 是 --> END
        C12 -- 是 --> C14[加样]
        C14 --> C15[启动清洗取样针<br/>不阻塞]
        C15 --> C16[移动到反应区<br/>与下一个样本并行]
        
        C16 --> C17{反应区满?}
        C17 -- 是 --> C18[暂停取样<br/>等待检测释放]
        C17 -- 否 --> C1
    end
    
    subgraph "检测流程（独立于取样状态机）"
        D1[反应区队列时间到] --> D2{仪器异常?}
        D2 -- 是 --> D3[跳过检测]
        D2 -- 否 --> D4[执行检测指令]
        D4 --> D5[更新检测结果]
        D5 --> D6{有暂停的取样?}
        D6 -- 是 --> D7[恢复取样流程]
        D6 -- 否 --> D8[结束]
    end
    
    subgraph "取样结束收尾"
        END --> E1[清洗取样针 + 样本架复位<br/>并行执行]
        E1 --> E2[通知 VM 取样结束]
        E2 --> E3{反应区队列为空?}
        E3 -- 是 --> E4[Completed 状态]
        E3 -- 否 --> E5[WaitingForTests 状态<br/>等待检测完成]
    end
    
    A4 --> B1
    B8 --> C1
    C18 --> D7
```

## 核心文件说明

| 文件 | 职责 |
|------|------|
| HomeModule/StateMachine/DetectionStateMachine.cs | 取样流程状态机，硬件指令下发，检测操作 |
| Platform/StateMachine/DetectionState.cs | 状态枚举定义 |
| Platform/StateMachine/DetectionTrigger.cs | 触发器枚举定义 |
| Platform/StateMachine/StateContext.cs | 状态上下文，标志位管理 |
| Platform/Model/Events/UIEvents.cs | UI 事件定义 |
| Platform/Model/Events/HardwareEvents.cs | 硬件回调事件定义 |
| HomeModule/ViewModels/HomeViewModel.cs | UI 事件转发，状态展示 |

## 设计原则

1. **状态机集中管控**：所有取样流程逻辑由 DetectionStateMachine 统一管理
2. **事件驱动**：通过事件邮箱通信，避免轮询
3. **VM 职责单一**：ViewModel 只负责 UI 展示和事件转发
4. **检测独立**：检测操作独立于取样状态机流转，只检查仪器异常
5. **并行优化**：取样+推卡并行，清洗+后续操作并行，移动反应区+下一个样本并行

# QC 检测流程架构（状态机设计）

## 架构概述

项目采用 **状态机 + 事件邮箱** 架构进行 QC 检测流程：

```
┌─────────────────────────────────────────────────────────────────┐
│                     QCViewModel                                 │
│  - UI 事件转发（点击开始质控）                                  │
│  - 接收状态机事件通知，更新 UI                                 │
│  - 质控操作调度（反应区队列时间驱动）                          │
└─────────────────────────────────┬───────────────────────────────┘
                                  │ 事件邮箱
                                  ▼
┌─────────────────────────────────────────────────────────────────┐
│                 QCStateMachine                                  │
│  - QC 检测流程状态机（Stateless 库）                           │
│  - 硬件指令下发与回调处理                                      │
│  - 通过事件通知 VM 更新 UI                                     │
│  - QC 检测操作（独立于状态流转，只检查仪器异常）              │
└─────────────────────────────────────────────────────────────────┘
```

## 业务状态（MachineStatus）

- 等待自检：None
- 已就绪：SelfInspectionSuccess / TestingEnd
- 检测中（取样阶段）：Sampling
- 取样完成：SamplingFinished
- 检测中（仅等待反应区检测任务）：Testing
- 自检失败：SelfInspectionFailed
- 运行错误：RunningError
- QC 检测：Sampling（使用 TestType 区分）

对应定义：FluorescenceFullAutomatic.Core\Config\MachineStatus.cs

## 状态机状态（QCState）

| 状态 | 说明 |
|------|------|
| Idle | 空闲状态 |
| VerifyingQCStart | 验证质控启动条件 |
| GettingMachineStatus | 获取仪器状态 |
| PushingCard | 推卡 |
| MovingToReactionArea | 移动到反应区 |
| Testing | 检测中 |
| WaitingForNextTest | 等待下次检测 |
| ProcessingResults | 处理检测结果 |
| CalculatingQCResults | 计算质控结果 |
| QCCompleted | 质控完成 |
| Error | 错误状态 |

## 状态机流转图

```mermaid
stateDiagram-v2
    [*] --> Idle
    
    %% 验证质控启动
    Idle --> VerifyingQCStart: StartQC
    VerifyingQCStart --> GettingMachineStatus: VerificationPassed
    VerifyingQCStart --> Idle: VerificationFailed
    
    %% 获取仪器状态
    GettingMachineStatus --> PushingCard: MachineStatusReceived
    GettingMachineStatus --> Error: ErrorOccurred
    
    %% 推卡流程
    PushingCard --> MovingToReactionArea: CardPushed
    PushingCard --> Idle: CardPushFailed
    PushingCard --> Error: ErrorOccurred
    
    %% 移动到反应区
    MovingToReactionArea --> Testing: ReactionAreaMoved
    MovingToReactionArea --> Error: ErrorOccurred
    
    %% 检测流程
    Testing --> ProcessingResults: TestCompleted
    Testing --> Error: ErrorOccurred
    
    %% 处理结果
    ProcessingResults --> Testing: NextTestReady
    ProcessingResults --> CalculatingQCResults: QCFinished
    
    %% 计算结果
    CalculatingQCResults --> QCCompleted: QCFinished
    
    %% 完成状态
    QCCompleted --> VerifyingQCStart: StartQC
    QCCompleted --> [*]
    
    %% 错误处理
    Error --> VerifyingQCStart: Retry
    Error --> Idle: StartQC
```

## 业务流程图

```mermaid
flowchart TD
    subgraph "QC 启动流程"
        A1[点击开始质控] --> A2{验证仪器状态}
        A2 -- 失败 --> A3[提示错误]
        A2 -- 通过 --> A4[获取仪器状态]
        A4 --> A5{验证状态<br/>卡仓/卡数/清洗液}
        A5 -- 失败 --> A6[提示重新获取]
        A5 -- 通过 --> A7[开始推卡]
    end
    
    subgraph "推卡与反应区"
        B1[推卡] --> B2{推卡结果}
        B2 -- 成功 --> B3[获取项目信息]
        B2 -- 失败 --> B4[重新推卡]
        B3 --> B5[移动到反应区]
        B5 --> B6[移动完成]
    end
    
    subgraph "QC 检测循环"
        C1[执行检测] --> C2{检测完成}
        C2 -- 完成 --> C3[处理检测数据]
        C2 -- 失败 --> C4[错误处理]
        C3 --> C5{达到检测次数?}
        C5 -- 否 --> C6[延时等待]
        C6 --> C1
        C5 -- 是 --> C7[计算质控结果]
    end
    
    subgraph "结果处理"
        D1[计算质控结果] --> D2[变异系数计算]
        D2 --> D3{是否合格?}
        D3 -- 合格 --> D4[显示合格结果]
        D3 -- 不合格 --> D5[显示不合格结果]
        D4 --> D6[结束流程]
        D5 --> D6
    end
    
    A7 --> B1
    B6 --> C1
    C7 --> D1
```

## 核心文件说明

| 文件 | 职责 |
|------|------|
| Main/ViewModels/QCViewModel.cs | UI 事件转发，状态展示，结果展示 |
| Main/StateMachine/QCStateMachine.cs | QC 检测流程状态机，硬件指令下发 |
| Platform/StateMachine/QCState.cs | QC 状态枚举定义 |
| Platform/StateMachine/QCTrigger.cs | QC 触发器枚举定义 |
| Platform/StateMachine/QCStateContext.cs | QC 状态上下文，标志位管理 |
| Platform/Model/Events/QCUIEvents.cs | QC 相关 UI 事件定义 |
| Platform/Model/Events/QCHardwareEvents.cs | QC 硬件回调事件定义 |

## 实现要点

1. **状态一致性**：确保状态机状态与 UI 状态保持一致
2. **错误处理**：统一的错误处理机制，支持重试
3. **数据管理**：检测结果、变异系数等数据的管理
4. **并行处理**：检测过程中的并行操作优化
5. **用户交互**：及时的 UI 反馈和状态更新
6. **流程控制**：QC 检测次数控制和结束条件判断
