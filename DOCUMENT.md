# Update Machine  
*tgm喜欢写文档, 但我一点都不喜欢*  

---  
## 使用Update Machine更新  
### 基本  
* 寻找nuget包`UpdateMachine.Core`并安装  

* 向原有程序开始之前 (例如`Main()`的起始) 加入
```csharp
#if !DEBUG
var updater = new Updater("YOUR_URL"); //填入oss上项目目录 (含有 .updatemachine 文件夹) 的公开地址
updater.Update();
#endif
```   
>完成更新的过程中可能会自动重启应用程序, 大约2次.  

### 更多  
#### 状态检查  
##### OnStatusChanged 事件  
更新器状态改变时, 上报`UpdaterStatus`类型.  
例: `updater.OnStatusChanged += status => Console.WriteLine(status)`  
  
status:  
* Check: 检查更新  
* Download: 下载更新  
* Copy: 安装更新  
* Start: 启动应用程序  

##### OnProgressChanged 事件  
更新器下载文件时, 上报两个`long`类型.  
例: `updater.OnProgressChanged += (current, total) => Console.WriteLine("{0}/{1}", current, total)`  
  
current: 已下载的文件数  
total: 总共需要下载的文件数  

##### OnException 事件  
更新器遇到异常时, 上报`Exception`类型.  
例: `updater.OnException += error => Console.WriteLine("Exception: {0}", error.Message)`  
  
error: 引发的异常  

>**带有UI的更新器**  
为更新器新建窗口, 并将其设置为启动窗口.  
在构造器中创建`Updater`, 并注册事件 (注意线程安全).  
*在新线程中`Update()`, 之后启动主窗口*. 

#### 异常处理
使用`try`以避免可能的应用程序崩溃 (大概是bug) .  
```csharp
try{ 
    updater.Update();
}catch{
  /*your code here*/
}
```

### 已知问题
* 网络异常时, 应用程序会循环更新, 无法启动.

## 使用UpdateMachine.Uploader上传更新  
### 初始设置  
* 在任意位置新建空文件夹, 将路径填入Uploader上方"工作区"中, 点击"加载". 此时下方界面会变为可用状态.  

* 填写OSS配置. 不知道怎么填的话, 我也没办法.

* 填写相对路径. **以`/`结尾, 不能以`/`开头**, 为空则上传到根目录.  

* 填写公开地址. 通常它是`http://<BUCKET_NAME>.<ENDPOINT>/<刚才填的路径>`. 你可以在OSS控制台上找到.   
>这些配置都会保存在当前工作区.  

### 上传
* 使用Release配置**重新生成**应用.

* 将生成的文件复制到工作区文件夹.  
>内置了UpdateMachine的程序会在启动时更新为服务器端版本. 因此重新生成之后**不要运行**应用.

* 启动上传器, 选择/填写工作区. 加载.

* 检查左侧文件列表, 确认没有多余/缺失文件.  

* 填写版本号, 更新记录, 点击上传.  

### 更多  
* 在项目属性中向"生成后事件命令行"填写下列内容, 可以在生成后自动复制文件.
```
xcopy /C /Y "$(ProjectDir)bin\Release\<YOUR_FILE_NAME>" "<工作区路径>\<YOUR_FILE_NAME>"
```
>替换<YOUR_FILE_NAME>和<工作区路径>为实际值.  
需要复制多个文件, 请填写多行.  
