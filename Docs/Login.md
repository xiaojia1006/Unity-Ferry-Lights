# 像素风账号登录界面

场景：`Assets/Scenes/Login.unity`。在 Unity 中打开后按 Play，或使用 `Tools > RPG > Login > Open Login Scene`。

村落、松树、角色、营火、羊皮纸、旗帜和按钮均复用项目现有素材。15棵背景/前景松树使用游戏内的 WillowPineFrame0–3 四帧、每秒5帧动画。场景中的 Canvas、文字、输入框、按钮、背景对象均可直接编辑；项目标题现为《渡口灯火》。界面以 1920×1080 为设计尺寸，使用 CanvasScaler 的 Expand 模式保持布局比例。

账号、密码均支持输入；密码默认掩码显示，右侧可显示/隐藏。Tab / Shift+Tab 在账号、密码和登录按钮间切换，Enter 提交。空账号、空密码会显示提示并聚焦输入框。点击“注册账号”切换为注册模式，填写账号（3至32位字母、数字或下划线）和密码（8至128位且不能全为空格），点击“创建账号”；成功后回到登录模式，再输入密码登录。账号不区分大小写，密码区分大小写，不会自动去除密码空格。本机服务暂未提供密码找回。

已接入 `Server/auth_server.py` 本机 HTTP 账号服务，监听 `127.0.0.1:18765`，仅供本机开发试玩。Unity 发起注册/登录请求，有10秒超时、错误提示及防重复提交。服务确认登录成功并返回有效账号和会话令牌后，加载上次保存的场景与位置（首次为 `RPG1`）；密码错误仍会拒绝进入。

启动时登录页会先请求 `/health` 探测服务；服务不可达时，账号输入为空直接点击进入世界会以"离线游客"身份试玩，进度独立保存在本机。会话仅保存在 LoginSession 内存中，不在磁盘保存密码或令牌。账号与 PBKDF2-SHA256 加盐密码哈希保存在 `Server/data/accounts.sqlite3`，服务重启后账号仍然存在；云端账号与联网游戏权限不属于此实现。

在 Unity 编辑器中从 Login 场景进入 Play 时，会尝试自动启动本机服务。也可使用 `Tools > RPG > Login > Start Local Account Service`，或在项目目录运行 `./Server/Start-AuthServer.ps1`。脚本优先使用 PATH 中的 Python，其次使用当前机器的 Codex Python，需要 Python 3.10+，无需第三方依赖。独立打包运行时须另外启动服务；此服务不会被自动打进游戏程序。

接口：POST `/register`、POST `/login`，JSON 请求字段为 account、password；GET `/health` 检查服务，GET `/me` 携带 `Authorization: Bearer <token>` 校验1小时内的内存会话。服务拒绝浏览器 Origin，限制请求体与每分钟请求数，不记录账号密码请求内容。

中文字体为本机 Windows 黑体的本地副本 LoginChinese.ttf，发布到其他设备前应确认字体授权或替换为有分发授权的中文字体。没有修改全项目的 TMP 默认字体。

自动创建仅在 Login 场景尚不存在时执行，不覆盖已有登录场景。构建顺序为 Login → RPG1 → RPG2 → Willowreach，登录目标使用场景名 RPG1，不依赖索引。升级前文件备份在 `Backups/LoginService`。第一版预览切换前保存了编辑器中原本未保存的 RPG1 编辑；保存前的磁盘版本另备份在 `Backups/LoginUI/RPG1-before-preview.unity`。

运行检查：进入 Login Play 模式，执行 `Tools > RPG > Login > Check And Capture In Play Mode`。结果保存在 `Logs/LoginChecks.txt`，截图保存在 `Logs/LoginPreview.png`。

完整联调：`Tools > RPG > Login > Check Register Login And RPG1`，测试原始树木动画、真实HTTP注册、错误密码拒绝、正确密码切场景及玩家初始化，结果在 `Logs/LoginServiceChecks.txt`。会创建一个以 qa_ 开头的随机测试账号。后端独立测试：`python Server/test_auth_server.py`，使用临时数据库，涵盖注册、重复账号、持久化、密码哈希、错误密码、输入验证、来源限制和请求限流。
