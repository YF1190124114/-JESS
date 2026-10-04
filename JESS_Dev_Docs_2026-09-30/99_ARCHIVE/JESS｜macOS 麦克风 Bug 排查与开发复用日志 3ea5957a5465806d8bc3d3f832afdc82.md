# JESS｜macOS 麦克风 Bug 排查与开发复用日志

> **JESS 工程排障记录｜macOS 麦克风权限 / Unity / Alibaba Cloud NLS ASR**
> 

<aside>
✅

**当前结论**

Standalone macOS App 的麦克风链路已经修复并实测成功：**macOS 麦克风 → Unity AudioClip → PCM16 → Alibaba Cloud NLS ASR → 文本**。

**注意：Unity Editor 内采样仍然为 0，目前没有修复。** 因此准确说法不是“Unity 麦克风问题全部解决”，而是“最终 Build 出来的 JESS App 已能正常调用麦克风并完成 ASR”。

</aside>

## 1. 最初的问题

JESS 的 Unity 项目可以检测到 MacBook Pro 麦克风，`Microphone Position` 也会增长，`AudioClip.GetData` 返回 `True`，AudioClip 状态为 `Loaded`。

但是实际读取出来的声音采样全部是 0：

- 最大音量：`0`
- 平均音量：`0`
- 阿里云请求：成功
- Alibaba NLS 状态：`20000000 / SUCCESS`
- 识别结果：空字符串

也就是说，当时最容易产生的误判是：

> “阿里云接口是不是坏了？”
> 

后来证明不是。**请求本身成功，但 Unity 实际发送的是静音数据。**

---

## 2. 排障过程

### 2.1 先确认 Unity 是否真的在运行新代码

通过在 `MicrophoneRecorder.cs` 中增加新的 Debug 日志，确认 Unity 实际执行的是修改后的脚本。

因此排除“Unity 仍在执行旧脚本 / 缓存版本”的可能。

### 2.2 检查 AudioClip 是否真的存在

增加日志：

- `AudioClip.GetData(...)` → `True`
- `AudioClip.loadState` → `Loaded`
- `Microphone Position` 会随着录音增长

说明 Unity 的 Microphone API 表面上确实启动了，AudioClip 也确实存在。

但是进一步直接统计 PCM 振幅后发现：

```
🔊 最大音量：0
🔊 平均音量：0
```

这一步非常关键。

**Microphone Position 在增长，并不能证明真的录到了声音。**

以后再遇到类似问题，应该直接检查 PCM sample，而不是只看 Microphone API 有没有运行。

### 2.3 排除麦克风设备选择问题

Unity 枚举到的设备只有：

```
[0] MacBook Pro麦克风
```

当前使用设备也是 MacBook Pro 内置麦克风。

因此不是选错输入设备。

### 2.4 排除硬件故障

macOS 自带语音备忘录能够正常录音。

所以：

```
MacBook 麦克风硬件 → 正常
macOS 音频输入 → 正常
```

问题继续缩小到 Unity / macOS 权限链。

### 2.5 排查采样率

通过：

```csharp
Microphone.GetDeviceCaps(microphoneDevice, out minFreq, out maxFreq);
```

得到设备报告的采样率范围约为：

```
最低：44100
最高：96000
```

因此曾把录音诊断采样率改为 `48000 Hz`。

但改为 48000 后 PCM 振幅依然全部为 0。

所以采样率问题不能解释“完全静音”。

### 2.6 Unity API 显示“有权限”，但 macOS 实际没有

代码中使用：

```csharp
Application.HasUserAuthorization(UserAuthorization.Microphone)
```

Unity 日志曾显示：

```
macOS 已授予 Unity 麦克风权限！
```

但是打开：

**系统设置 → 隐私与安全性 → 麦克风**

里面根本没有 Unity / Unity Editor。

这成为一个重要经验：

> **Unity 自己报告 HasUserAuthorization = true，不等于 macOS TCC 权限链一定已经正确建立。**
> 

系统隐私设置 + 实际 PCM 数据，比单独一个 Unity API 返回值更可信。

---

## 3. 转折：不再死磕 Editor，直接测试最终 macOS App

为了区分：

```
Unity Editor 的问题
vs
最终产品运行环境的问题
```

我们构建了 macOS Standalone App：

```
JESS_MacBuild.app
```

然后检查构建产物真正的：

```
Contents/Info.plist
```

执行：

```bash
/usr/libexec/PlistBuddy -c "Print :NSMicrophoneUsageDescription" "/Users/guoxiaoguang/Desktop/JESS_MacBuild.app/Contents/Info.plist"
```

得到：

```
Print: Entry, ":NSMicrophoneUsageDescription", Does Not Exist
```

### 关键发现

虽然 Unity Player Settings 里已经填写：

```
Microphone Usage Description
JESS needs microphone access for voice interaction.
```

但是最终 Build 出来的 App 的 `Info.plist` 里，**实际没有 `NSMicrophoneUsageDescription`。**

这就是这次排障真正的突破口。

---

## 4. 修复 Standalone App

### 4.1 手工加入 NSMicrophoneUsageDescription

执行：

```bash
/usr/libexec/PlistBuddy -c "Add :NSMicrophoneUsageDescription string 'JESS needs microphone access for voice interaction.'" "/Users/guoxiaoguang/Desktop/JESS_MacBuild.app/Contents/Info.plist"
```

随后再次读取，已经能够看到：

```
JESS needs microphone access for voice interaction.
```

### 4.2 修改 App 后重新签名

修改 App Bundle 内容以后，需要重新 codesign。

第一次执行：

```bash
codesign --force --deep --sign - "/Users/guoxiaoguang/Desktop/JESS_MacBuild.app"
```

报错：

```
resource fork, Finder information, or similar detritus not allowed
```

### 4.3 检查扩展属性

使用：

```bash
xattr -lr "/Users/guoxiaoguang/Desktop/JESS_MacBuild.app"
```

发现 App 和 `MainMenu.nib` 上存在 `com.apple.FinderInfo` 等扩展属性。

因此先复制一个干净测试包：

```bash
ditto --noextattr --noqtn "/Users/guoxiaoguang/Desktop/JESS_MacBuild.app" "/tmp/JESS_MacBuild.app"
```

然后删除剩余 FinderInfo：

```bash
xattr -d com.apple.FinderInfo "/tmp/JESS_MacBuild.app/Contents/Resources/MainMenu.nib"
xattr -d com.apple.FinderInfo "/tmp/JESS_MacBuild.app"
```

再次执行：

```bash
xattr -lr "/tmp/JESS_MacBuild.app"
```

无输出，说明扩展属性已经清干净。

### 4.4 重新签名成功

执行：

```bash
codesign --force --deep --sign - "/tmp/JESS_MacBuild.app"
```

得到：

```
/tmp/JESS_MacBuild.app: replacing existing signature
```

没有报错。

### 4.5 重置 macOS 麦克风授权状态

确认 Bundle ID：

```
com.DefaultCompany.jessv0
```

执行：

```bash
tccutil reset Microphone com.DefaultCompany.jessv0
```

返回：

```
Successfully reset Microphone approval status for com.DefaultCompany.jessv0
```

### 4.6 启动修复后的 App

```bash
open "/tmp/JESS_MacBuild.app"
```

进入 JESS 后真正触发 `Microphone.Start`。

这一次 macOS **终于正式弹出麦克风权限请求**。

点击“允许”。

此后 JESS 构建版出现在：

**系统设置 → 隐私与安全性 → 麦克风**

而 Unity / Unity Editor 目前仍然没有出现在这里。

---

## 5. 最终实际效果

修复后的 Standalone App 真实录音日志：

```
🔴 开始录音...
🎧 AudioClip 实际采样率：16000
🎧 AudioClip 实际声道数：1
🎧 Microphone Position：109448
🎧 AudioClip 总采样帧数：480000
⏹️ 录音结束：6.84 秒
🧪 AudioClip loadState：Loaded
🔊 最大音量：1
🔊 平均音量：0.369897
☁️ 正在发送给阿里云识别...
✅ 阿里云返回：
🗣️ 你刚才说的是：呃，你好，James, 这里是麦克风电视。
```

因此我们得到了完整的端到端证据：

```
人的声音
   ↓
MacBook Pro 麦克风
   ↓
macOS 麦克风权限
   ↓
Unity Microphone
   ↓
AudioClip
   ↓
PCM16
   ↓
Alibaba Cloud NLS
   ↓
识别文字
```

**ASR 第一阶段已经真实跑通。**

识别把 JESS 听成 James、把“测试”听成“电视”，属于后续识别精度优化，不是链路故障。

---

## 6. 这次 Bug 最值得复用的经验

1. **检测到麦克风 ≠ 录到了声音。** `Microphone.devices` 有设备只能说明枚举成功。
2. **Microphone Position 增长 ≠ PCM 有效。** 一定要直接统计 sample 最大值 / 平均绝对振幅。
3. **HTTP 成功 ≠ 上游音频正确。** ASR 可以成功接收一段静音并返回空 result。
4. **框架层权限状态 ≠ 操作系统真实权限状态。** macOS 上应同时看 TCC / 系统隐私设置以及真实音频数据。
5. **不要只相信 Unity Player Settings。** 最终交付的是 App Bundle，所以必须检查 Build 后真正的 `Info.plist`。
6. **修改 .app 后要考虑签名。** macOS 的 codesign 和扩展属性会影响修改后的 App。
7. **排障应该分层。** 硬件 → OS 权限 → Unity 采集 → PCM → 网络 → ASR，逐层证明，不要同时乱改多个变量。

---

## 7. Unity Editor 仍然没有修好，怎么办？

目前真实状态：

| 环境 | 麦克风 | ASR |
| --- | --- | --- |
| Unity Editor | ❌ PCM 仍为 0 | 无法真实语音测试 |
| JESS macOS Build | ✅ 正常 | ✅ 阿里云识别成功 |

这并不阻塞 JESS 的继续开发。

因为最终产品本来就是 Build 后的 App，而不是 Unity Editor。

---

## 8. 不修 Unity Editor 麦克风时的开发方案

JESS 应该按模块接口开发，而不是要求所有模块每时每刻都真实连接。

```
                 JESS
                  │
     ┌────────────┼────────────┐
     ↓            ↓            ↓
   Speech        Brain        Body
   声音模块       大脑模块      身体模块
     │            │            │
麦克风 / ASR     LLM / 思考    表情 / 动作
TTS              人格 / 记忆    Animator
     │            │            │
     └────────────┼────────────┘
                  ↓
              Unity 集成
                  ↓
           Build macOS App
```

### 8.1 Speech / ASR

Speech 模块只需要保证：

```
声音 → string
```

例如最终对外输出：

```
"你好 JESS"
```

独立验收标准：

> Build macOS App → 说话 → Alibaba NLS 返回非空 `result`。
> 

Speech 开发时不需要 Brain 存在。

### 8.2 Brain

Brain 根本不需要真实麦克风才能开发。

直接用测试字符串：

```
你好 JESS，我今天有点累
```

模拟 ASR 输入。

Brain 输出统一 JSON：

```json
{
  "reply": "那今天别把自己逼得太紧。",
  "expression": "concerned",
  "action": "look_at_user"
}
```

因此 Brain 开发者可以完全独立工作。

### 8.3 Body

Body 在 Unity Editor 里直接测试：

```
expression = happy
action = wave
```

然后观察：

- BlendShape
- Animator
- 动作切换
- 表情状态

Body 不需要麦克风，也不需要真实 LLM。

### 8.4 TTS

直接给 TTS 固定字符串：

```
你好呀，我是 JESS。
```

测试：

```
string → TTS → AudioSource → JESS说话
```

同样不依赖 ASR。

---

## 9. 最终集成方式

最后母项目只负责把这些接口连起来：

```
用户说话
   ↓
Speech / ASR
   ↓
userText
   ↓
Brain
   ↓
{
 reply,
 expression,
 action
}
   ↓
┌──────────┬────────────┐
↓          ↓            ↓
TTS     Expression    Action
↓          ↓            ↓
说话       表情          动作
```

这样任何一个模块坏掉，都可以单独替换或测试，而不是整个 JESS 一起坏。

---

## 10. 推荐测试层级

| 测试层级 | 测试内容 | 目的 |
| --- | --- | --- |
| 模块测试 | 固定字符串、假 JSON、直接调用动作 | 快速开发，不依赖其他模块 |
| Unity Editor | Brain / Body / TTS 等非麦克风功能 | 高频开发调试 |
| Standalone Build | 真实麦克风 + ASR + 最终全链路 | 验证最终产品环境 |

这意味着以后不用为了测试 Brain 而录一次音，也不用为了测试一个表情重新 Build 整个 App。

只有需要验证：

> **真实麦克风 / macOS 权限 / 最终完整链路**
> 

时才 Build。

---

## 11. 下一步必须工程化：自动修改 Info.plist

现在成功的 `/tmp/JESS_MacBuild.app` 是一个**诊断测试产物**。

有两个问题：

1. `/tmp` 可能被系统清理；
2. `NSMicrophoneUsageDescription` 是我们手工加入的。

因此不能把：

> “每次 Build → Terminal 手动改 plist → 清 xattr → codesign”
> 

当成正式开发流程。

后续应该给 Unity 增加一个 **macOS Post-build Script**：

```
Unity Build
    ↓
Post-build Script
    ↓
自动打开 Info.plist
    ↓
检查 NSMicrophoneUsageDescription
    ↓
不存在 → 自动写入
    ↓
生成可正常请求麦克风权限的 JESS.app
```

最终目标是：

> **以后只需要点一次 Build，生成的 JESS App 自己就是正确的。**
> 

---

## 12. 是否还值得修 Unity Editor？

值得，但**不再是阻塞项**。

如果以后把 Unity Editor 权限也修好，开发体验当然更好，因为 Speech 也能直接在 Editor 里测试。

但是项目推进优先级应该是：

```
Standalone ASR 已通 ✅
        ↓
Brain
        ↓
TTS
        ↓
Body
        ↓
完整 JESS 链路
        ↓
自动化 Build / plist
        ↓
有时间再继续研究 Unity Editor TCC
```

不要为了一个只影响开发便利性的 Editor 权限问题，再阻塞整个数字人的开发。

---

## 13. 当前项目状态快照

- [x]  MacBook 麦克风硬件正常
- [x]  Alibaba Cloud NLS 接口可用
- [x]  JESS Standalone App 获得 macOS 麦克风权限
- [x]  Unity Standalone 能读取非零 PCM
- [x]  Alibaba NLS 能返回真实识别文字
- [x]  Speech / ASR 第一阶段完成
- [ ]  Unity Editor 麦克风采样仍为 0
- [ ]  Build 后自动写入 Info.plist
- [ ]  Brain 接入
- [ ]  TTS 接入
- [ ]  Expression 接入
- [ ]  Action 接入
- [ ]  Speech → Brain → TTS / Body 全链路集成

<aside>
🧠

**一句话总结这次排障**

问题不是“阿里云听不懂”，而是 **Unity Editor / macOS 权限链导致 Unity 实际拿到的是全 0 音频**。通过检查最终 Build 的 `Info.plist`，发现缺少 `NSMicrophoneUsageDescription`；手动补入、清理扩展属性、重新签名并重置 TCC 后，Standalone JESS App 正常触发 macOS 麦克风授权，并成功完成真实语音 → PCM → Alibaba NLS → 文本的完整链路。

</aside>