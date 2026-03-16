# InputFramework
额外输入框架Mod
这个Mod是一个框架Mod，用于为其他Mod引入方便的按键事件注册功能
通过这个Mod注册的按键将会使用与游戏原本按键完全一致的输入框架处理，并且能在原版按键绑定界面显示并绑定
所有引入的额外按键都将被归入Debug类中

开发者如何使用本框架：
框架提供：
public static class ExtraInputManager :: public static void RegisterAction(string name, InputActionType type, string category = null)
调用示例：
ExtraInputManager.RegisterAction("YourMod::YourAction", InputActionType.Button);
其中Action的名字作为唯一标识符，结构"YourMod::YourAction"仅为推荐，你可以使用任意独特的字符串，若字符串重复，框架将跳过并不注册你的Action
category为可选，默认将归类为Debug。由于游戏本身的结构问题，建议不要指定，若指定，请保证指定的category存在

框架会在Rewired启动时一次性注册全部Action到Rewired。存在一些Corner Case，你的Mod向框架注册Action时Rewired已经启动，
为了解决此类问题，框架会持久性记忆全部注册到框架的Action，并在启动时加载，因此重启即可让没有来得及注册到Rewired成功注册。
若向框架注册Action时Rewired已经启动，框架将打印警告日志，提示用户应当重启。

注册完毕后，你就可以使用Rewired提供的GetAction/GetButton以及其衍生方法进行输入处理了：

示例：在关卡场景中，持续检测一个按键和一个修饰键：

void HandleInput()
{
    if(GameManager.gameState != GameState.Multiplayer && GameManager.gameState != GameState.SinglePlayer) return; // 仅战斗场景响应
    Rewired.Player player = ReInput.players.GetPlayer(0);
    bool modifierPressed = player.GetButton("YourMod::YourActionModifier");

    if (player.GetButtonDown("YourMod::YourAction"))
    {
        if (modifierPressed)
        {
            //Get combination
            //Do something
        }
        else
        {
            //Get single key
            //Do something
        }
    }
}

注意：
1. 受限于Rewired机制，目前引入的Action无法添加默认绑定，因此新加一个Mod/用户执行了恢复默认绑定时，Action将会没有按键/轴绑定
2. 关于Shift + 小键盘数字键的组合，此类组合会在输入上将被Rewired判定为输入符号，因此将无法正确触发按键检测。要使用这种组合，请做特殊处理