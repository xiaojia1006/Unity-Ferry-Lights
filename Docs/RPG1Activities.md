# RPG1 林间试炼

在原 RPG1 场景上增加独立根节点 `RPG1 Activities - editable posts`，不重建原有地图、商人、树动画、敌人、背包或摄像机。使用项目已有石头、按钮、丝带与羊皮纸素材。

## 操作与规则

- 右下角小地图显示真实河流轮廓、玩家（红色菱形）、商人、委托牌与任务点。Tab 展开地图，点击标记看说明，Tab/Esc/关闭按钮返回。打开地图时通过 MenuPause 暂停，不影响其他菜单持有的暂停。
- 靠近委托牌按 E 开始巡路，150 秒内依次到 1、2、3 按 E 盖章。三章齐全后计时停止，回委托牌按 E 获得 18 金币、6 经验。失败免费重试，不扣材料。菜单暂停期间不计时。
- 靠近符石按 E 观看四次闪光/编号提示，再按顺序触碰四次。F4 可重播，答错无损失。成功获得 15 金币、5 经验。
- 击败 RPG1 中 3 个敌人后回委托牌按 E，获得 12 金币、4 经验。不另算击杀本身的经验奖励。
- 每项任务每次运行只发一次奖励；跨场景保留已领奖状态及讨伐计数。尚未交付的巡路/记忆挑战离开场景后重置。本任务的巡路/符石/讨伐状态尚未纳入按账号 `SaveSystem`，停止并重新运行游戏会重置；RPG1 Frontier 与 RPG2 的世界进度已接入存档。
- 奖励通过原 InventortyManager 的金币/经验通道发放，不占背包格，不会因背包已满丢失物品。

## 编辑

场景里每个任务点可直接移动，Posts 数组对应委托牌、3 个巡路点、3 个符石。保持地面连通、避开河道/碰撞物，不要把任务点藏在树后。编辑地图地形后，停止运行，执行 Tools > RPG > RPG1 Activities > Install And Open RPG1 重新生成地图和安全任务位置（会重设自定义任务点位置）。

安装时用半米网格对原始碰撞体和河流轮廓做陆地连通搜索，预留角色通行空间。任务标记本身不阻塞道路。生成报告位于 Logs/RPG1ActivitiesPlacement.txt。

## 校验

Tools > RPG > RPG1 Activities > Verify In Play 验证任务顺序、奖励限领、符石错误处理及引用。测试临时替换经验奖励通道，避免改变玩家等级；结束恢复金币、奖励引用和任务状态。报告 Logs/RPG1ActivitiesChecks.txt。另进行了 Unity Game 画面检查和编辑器工程编译。

## 回滚

备份目录由 Backups/LatestRPG1Activities.txt 指向。本次为 Backups/RPG1Activities-20261117-155424。

1. 停止 Play，关闭/卸载场景，备份当前结果。
2. 用备份目录中的 RPG1.unity 覆盖 Assets/Scenes/RPG1.unity（保留原 .meta/GUID）。这是修改前的原场景，不要用中途产生的 RPG1-live.unity 代替。
3. 用备份 WillowreachFieldMap.cs 覆盖 Assets/scrpits/WillowreachFieldMap.cs，可撤销标题修正。
   用备份 StatsUI.cs 覆盖 Assets/scrpits/PlayerScripts/StatsUI.cs，可撤销地图关闭时避免 Esc 同时打开属性面板的输入保护。
4. Willowreach-live.unity 保存了开始操作时 Unity 尚未保存的 Willowreach 场景状态；需要完整恢复该场景时用它覆盖 Assets/Scenes/Willowreach.unity，保留该场景原 .meta。
5. 新增 RPG1Activities.cs、Editor/RPG1ActivitiesSetup.cs、Editor/RPG1ActivitiesChecks.cs 与 SceneExpansion/RPG1FieldMap.png 可一并移除及对应 .meta；不要删除其他既有素材。

地图标题修正：柳湾边境地图取消人为字间空格，并按可见字形居中，针对丝带透明区域上移 8 UI 像素；旅行手记标题同样按字形校正。
