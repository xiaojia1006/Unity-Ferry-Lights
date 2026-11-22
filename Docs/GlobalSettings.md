# 全局设置

入口：所有场景右上角“设置 Esc”，或 Esc / F10 开关。背包、地图、商店、对话打开时 Esc 先归它们，不会叠开设置面板。

- 音频：主音量通过 AudioListener 控制；音乐单独控制持续播放的程序合成背景音乐；音效控制菜单音与现有环境音；环境声开关控制雷声；登录音乐开关在 Login 场景生效。
- 画面：独立游戏全屏/窗口、垂直同步、30/60/120 FPS 上限。垂直同步优先跟随显示器；全屏在 Unity 编辑器内不切换编辑器窗口。
- 操作：双键位、仅 WASD、仅方向键，实际接入 PlayerMovement。
- 游戏：实时 FPS 显示。
- 账号：显示 LoginSession 账号；二次确认后结束本次运行、释放持久游戏对象并回到 Login，设置保留。游戏任务进度没有新增磁盘存档功能。
- 恢复默认：只重置本机设置，不删除账号。

实现：Assets/scrpits/GlobalSettings.cs 在 BeforeSceneLoad 启动并 DontDestroyOnLoad，不依赖某个场景中的对象。设置使用 PlayerPrefs 的 Rpg.GlobalSettings.v1 键；不保存密码或登录令牌。MenuPause 负责嵌套暂停；GlobalSettings.BlocksInput 防止穿透到背包、委托、对话和装备快捷键。

中文字体：Resources/Settings/Chinese.asset。运行时不依赖 UnityEditor。

验证桥：Assets/Editor/SettingsVerification.cs；请求 Temp/Settings.request。运行检查结果见 Logs/SettingsChecks.txt；界面预览 Logs/SettingsPreview.png。

验证完成（2026-11-22）：Unity 脚本编译通过；运行检查涵盖主音量滑块、静音、环境声关闭、垂直同步/目标帧率 API、本机设置序列化、打开/关闭暂停和嵌套菜单暂停、中文资源加载。Login/RPG1/RPG2 切换后保持单实例与设置值；切换账号清空 Account/Token 并返回 Login，时间恢复为 1。最终 UI 预览已检查，滑块和标签无重叠。全屏切换需要在独立游戏中验证。
