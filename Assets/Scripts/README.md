# 2D回合制战斗游戏系统说明

## 工程结构

本游戏是一个2D回合制战斗游戏，玩家通过操作棋盘上的彩色格子来攻击敌人。

## 核心系统

### 1. GameManager (`GameManager.cs`)
- **功能**: 游戏主管理器，控制游戏主逻辑
- **职责**: 
  - 初始化所有管理器（MainGameManager、BoardManager、EnemyManager）
  - 提供开始新游戏、重新开始等接口
  - 使用单例模式（继承自`Singleton<T>`）

### 2. MainGameManager (`MainGameManager.cs`)
- **功能**: 一场战斗的游戏控制核心
- **职责**:
  - 回合制逻辑管理（玩家回合、敌人回合）
  - 处理玩家操作：
    - **鼠标左键拖动**: 交换相邻的两个格子
    - **鼠标右键点击**: 消除鼠标所在位置和周围（上下左右连通）的同色格子
  - 消除后的处理：
    - 格子向左掉落填补空缺（重力系统）
    - 在消除位置创建波浪Prefab，向右飞去攻击敌人
  - 玩家操作后，控制所有敌人向左移动
  - 刷新新敌人
  - 检测游戏结束条件（敌人到达最左侧）

### 3. BoardManager (`BoardManager.cs`)
- **功能**: 管理6x8（可调节）的战斗棋盘
- **职责**:
  - 初始化棋盘（可设置宽高）
  - 清空棋盘
  - 随机生成格子颜色（红黄蓝绿四种，可调节）
  - 处理格子交换
  - 查找连通同色格子（BFS算法）
  - 处理重力系统（格子掉落填补空缺）
  - 坐标转换（网格坐标 ↔ 世界坐标）

### 4. TileCell (`TileCell.cs`)
- **功能**: 棋盘格子类
- **职责**:
  - 存储格子颜色和网格位置
  - 更新视觉表现
  - 提供动画接口（交换、掉落、消除）

### 5. TileColor (`TileColor.cs`)
- **功能**: 格子颜色枚举和工具类
- **颜色**: Red（红）、Yellow（黄）、Blue（蓝）、Green（绿）
- **工具**: 提供颜色转换和随机颜色生成

### 6. EnemyManager (`EnemyManager.cs`)
- **功能**: 敌人管理器
- **职责**:
  - 在棋盘右半部分随机生成敌人（数量可控制）
  - 所有敌人向左移动
  - 刷新新敌人（每回合后）
  - 检测是否有敌人到达最左侧

### 7. Enemy (`Enemy.cs`)
- **功能**: 敌人系统
- **属性**:
  - 血量系统（受到攻击掉血）
  - 击退效果（受攻击时被击退）
  - 移动系统（向左移动）
  - 死亡检测
  - 到达边缘检测

### 8. Wave (`Wave.cs`)
- **功能**: 波浪攻击系统
- **职责**:
  - 从消除位置向右飞出
  - 碰撞检测（使用Trigger）
  - 对碰撞的敌人造成伤害并触发击退
  - 自动销毁

## 游戏流程

1. **游戏开始**:
   - 清空棋盘
   - 随机生成格子颜色
   - 在右半部分随机生成敌人

2. **玩家回合**:
   - 玩家可以：
     - 鼠标左键拖动交换两个相邻格子
     - 鼠标右键消除同色连通格子
   - 消除后：格子掉落填补 → 创建波浪攻击敌人

3. **敌人回合**:
   - 所有敌人向左移动一格
   - 刷新一个新敌人（在最右侧）

4. **游戏结束**:
   - 当有敌人移动到最左侧时，游戏结束

## 使用说明

### 在Unity中设置：

1. **创建GameManager对象**:
   - 在场景中创建一个空GameObject，命名为"GameManager"
   - 添加`GameManager`组件

2. **创建BoardManager对象**:
   - 创建空GameObject，命名为"BoardManager"
   - 添加`BoardManager`组件
   - 设置`tileCellPrefab`（需要一个带有SpriteRenderer和TileCell组件的Prefab）
   - 调整棋盘参数（宽度、高度、格子大小等）

3. **创建MainGameManager对象**:
   - 创建空GameObject，命名为"MainGameManager"
   - 添加`MainGameManager`组件
   - 拖拽BoardManager和EnemyManager的引用
   - 设置`wavePrefab`（需要带有Collider2D和Wave组件的Prefab）

4. **创建EnemyManager对象**:
   - 创建空GameObject，命名为"EnemyManager"
   - 添加`EnemyManager`组件
   - 设置`enemyPrefab`（需要带有SpriteRenderer、Collider2D和Enemy组件的Prefab）
   - 设置敌人生成参数（最小/最大数量等）

5. **创建Prefab**:
   - **TileCell Prefab**: 需要有SpriteRenderer和TileCell组件
   - **Enemy Prefab**: 需要有SpriteRenderer、Collider2D（设置为Trigger）和Enemy组件
   - **Wave Prefab**: 需要有SpriteRenderer、Collider2D（设置为Trigger）和Wave组件

### 参数配置

- **BoardManager**:
  - `boardWidth`: 棋盘宽度（默认8）
  - `boardHeight`: 棋盘高度（默认6）
  - `tileSize`: 格子大小（默认1）
  - `availableColors`: 可用的颜色列表

- **EnemyManager**:
  - `minEnemyCount`: 最小敌人数量
  - `maxEnemyCount`: 最大敌人数量

- **Enemy**:
  - `maxHealth`: 最大血量
  - `moveSpeed`: 移动速度
  - `knockbackForce`: 击退力度
  - `knockbackDuration`: 击退持续时间

- **Wave**:
  - `moveSpeed`: 移动速度
  - `damage`: 伤害值
  - `range`: 攻击范围

## 依赖

- **DOTween**: 用于所有动画效果（交换、掉落、消除、移动、击退等）
- **Unity 2D**: 使用2D Sprite进行渲染

## 扩展建议

1. 添加更多颜色种类
2. 添加特殊格子（炸弹、连锁消除等）
3. 添加敌人类型（不同血量、移动速度）
4. 添加技能系统
5. 添加关卡系统
6. 添加音效和粒子效果
7. 优化对象池系统（回收波浪和敌人）


