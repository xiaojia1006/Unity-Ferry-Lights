# 晴、雨、雪随机天气

适用于本工程 Unity 2021.3 内置管线、XY 平面、正交相机和 3/4 像素美术。沿用现有雪面与独立河流遮罩，不改变相机、角色、河道轮廓、碰撞或商家金币。

RPG1 / RPG2 的 `Winter Weather` 对象现在附带 `Weather Cycle 2D`：

- `Current Weather`：Sunny 晴、Rain 雨、Snow 雪。编辑模式立即预览；运行模式渐变到所选天气。
- `Random Weather`：勾选后运行时自动随机切换；编辑模式不会自动切换。
- `Minimum Duration / Maximum Duration`：两次切换之间的随机秒数，默认 45–100 秒（含过渡）。
- `Transition Seconds`：默认 6 秒；0 为立即切换。过渡中可能短暂雨夹雪。
- `Sunny / Rain / Snow Weight`：默认 5 / 3 / 2。排除当前天气后，按剩余权重选择下一种，不连续重复。0 不参与抽取；若没有可选项就保持当前天气。
- `Rain Intensity`：雨量；雪量、积雪与风力继续在 `Winter Weather 2D` 调节。

默认首次启动为晴天；同一次运行切换 RPG 场景会延续天气、倒计时和过渡进度。游戏时间暂停也会暂停天气计时和粒子。禁用随机开关可手动固定天气。

菜单 `Tools > RPG > Weather > Preview Sunny / Rain / Snow` 可以直接预览并选中天气对象；也可使用组件右上角菜单 Preview。场景保留旧名称，避免现有引用丢失。编辑后正常保存场景即可，不会自动保存或重载场景。

晴天恢复原地面材质。雨天使用像素雨丝、地表湿润暗色与仅在河道遮罩内绘制的雨滴圆形涟漪。雪天保留积雪、分层雪花和装饰岸冰。天气只改变视觉，没有新增可行走冰面、移动惩罚、音效、雷电或昼夜系统；屋内场景需另行配置遮雨区域。

勾选原 `Preview In Editor` 才显示 Scene 降水。关闭 `Weather Cycle 2D` 停止系统并回晴；关闭 `Winter Weather 2D` 则关闭所有天气视觉。已有粒子在过渡时会自然消散。

验证菜单：`Tools > RPG > Weather > Check Weather Cycle`，报告 `Logs/WeatherCycle2DChecks.txt`。覆盖随机轮换、权重、暂停、过渡、雪雨互斥终态、材质恢复、河道遮罩和碰撞。
