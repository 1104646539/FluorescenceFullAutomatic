# QC 模块测试需求文档

本文档描述了 `FluorescenceFullAutomatic` 项目中 QC (Quality Control) 模块的测试需求、测试用例设计以及自动化测试指导。

## 1. 概述
QC 模块负责仪器的质控流程，包括硬件控制、数据采集、变异系数计算和结果判定。主要核心类为：
- `QCViewModel`: 负责 UI 交互、数据展示和事件响应。
- `QCStateMachine`: 负责质控流程的状态流转和硬件指令交互。

## 2. 测试环境
- **测试框架**: MSTest
- **Mock 框架**: Moq
- **项目**: `TestMain`

## 3. 测试用例设计

### 3.1 QCViewModel 测试
主要关注 UI 逻辑、命令可用性校验和结果计算。

| 测试场景 | 前置条件 | 操作 | 预期结果 |
| :--- | :--- | :--- | :--- |
| **启动质控校验** (成功) | 仪器待机，反应区为空 | 调用 `ClickStartQC` | 发送 `StartQCEvent`，清空旧结果 |
| **启动质控校验** (失败-自检) | 仪器状态 `SelfInspectionFailed` | 调用 `ClickStartQC` | 弹出错误提示，不发送事件 |
| **启动质控校验** (失败-忙碌) | 仪器状态 `Camping` | 调用 `ClickStartQC` | 弹出错误提示 |
| **结果计算** (合格) | 模拟 10 次检测数据，方差 < 5% | 触发 `QCFinishEvent` | `Variance` 计算正确，Result 为"合格" |
| **结果计算** (不合格) | 模拟 10 次检测数据，方差 > 5% | 触发 `QCFinishEvent` | `Variance` 计算正确，Result 为"不合格" |

### 3.2 QCStateMachine 测试
主要关注状态流转逻辑和硬件交互异常处理。

| 测试场景 | 当前状态 | 触发/事件 | 预期行为 |
| :--- | :--- | :--- | :--- |
| **开始质控** | Init | Fire `StartQC` | 进入 `PreparingQC` 状态 |
| **确认加卡** | PreparingQC (等待加卡) | 调用 `HandleCardAddedConfirm` | 发送推卡指令，进入 `PushingCard` |
| **硬件错误处理** | Any | 串口超时/错误 | 触发 `HandleSerialError`，发送错误事件，中止流程 |

## 4. 扩展指南
- 添加新测试时，请确保 Mock 所有外部依赖 (`ISerialPortService` 等)。
- 涉及多线程/Dispatcher 的回调，需 Mock `IDispatcherService` 直接执行 Action。
- 状态机测试较为复杂，需仔细模拟硬件反馈的异步流程。
