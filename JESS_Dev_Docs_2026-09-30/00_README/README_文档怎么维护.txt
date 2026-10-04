JESS 开发文档结构说明
首次整理：2026-09-30
最近维护：2026-10-05

这次整理的原则是：把“发生过什么”与“以后怎么用”分开。

01_DEBUG
记录 Bug 是怎么出现、怎么排查、为什么修好。
适合以后遇到类似问题时回溯。

02_ARCHITECTURE
记录当前长期设计边界。
例如 Brain / Controller / Driver / Model 如何解耦。
这类内容不应该埋在某次 Bug 日志里。

03_RUNBOOK
记录“以后真要做这件事时，照什么步骤操作”。
当前已经单独提取：
- 模型替换与 Blender 接入
- macOS 麦克风与 Build

04_CURRENT_STATE
只维护一份最新状态快照：
现在什么已通、什么没通、下一步是什么。
避免以后从几十页 Debug Log 猜当前工程状态。

99_ARCHIVE
保存用户原始日志，不删、不覆盖。
整理版若有遗漏，可以回到这里查原始材料。

建议以后新增内容时：
- 出 Bug → 写 01_DEBUG
- 架构决策变化 → 更新 02_ARCHITECTURE
- 形成可复用操作流程 → 更新/新增 03_RUNBOOK
- 每完成一个阶段 → 更新 04_CURRENT_STATE

不要再把 Debug、架构说明、使用说明、当前状态全部塞进同一份日志。

Git / GitHub 开发记录原则：
- main 只记录已经合并的稳定开发基线；
- 新功能从最新 main 建立 feature / refactor branch；
- 完成后 commit、push、创建 PR、merge 回 main；
- 合并后清理 feature branch；
- CURRENT_STATE 记录“当前 main 已经具备什么”；
- DEBUG 记录验证过程和临时代码的去留；
- ARCHITECTURE 记录长期边界，不把短期实验写成永久设计。

Secret 安全原则：
- 文档中不得写入 API Key、AccessToken、密码或其他真实凭据；
- 不从 LocalSecrets 复制任何实际值到文档；
- 可以记录“凭据由本地配置提供”“Token 可能过期”等事实；
- 截图和日志进入文档前必须先确认已遮挡敏感信息。

目录名说明：
JESS_Dev_Docs_2026-09-30 已经从一次性快照发展为持续维护文档。
建议后续在单独操作中更名为 JESS_Dev_Docs，
但本轮不自行更名，避免影响现有引用和工作流。
