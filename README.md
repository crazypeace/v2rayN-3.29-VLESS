# v2rayN-3.29-VLESS
A v2rayN-3.29 mod version. Support VLESS Reality. Support TLS Hello fragment. Support Hysteria2.  
v2rayN-3.29 修改版, ✅VLESS Reality, ✅TLS fragment 分片, ✅Socks 下一跳出口, ✅Hysteria2

![image](https://github.com/crazypeace/v2rayN-3.29-VLESS/assets/665889/dde23c73-6885-47bf-8006-9f6ed3ef14a9)
<img alt="v2rayN_2024-11-09_02-44-43" src="https://github.com/user-attachments/assets/f5f8b72a-7448-4ff3-be63-f7dcb15156bb">


# 本repo的目的
- 保留v2rayN 3.29的PAC功能
- 从剪贴板添加VLESS节点
- 复制VLESS节点分享链接
- 二维码显示VLESS节点分享链接
- 订阅设置 增加 `Set TLS allowInsecure option to True` 的设置项，方便使用[机场订阅](https://1ladder.eu.org/bwgvpn)
- 支持 VLESS Reality
- 支持 Socks 出口
- 支持 TLS Hello 分片
- 支持 Hysteria2

# 限制 
- 不能同时选取 Reality 和 Hy2 节点批量测速.
- 如果你是使用 v2rayN.zip 并自己添加内核的话, 请不要放入 V2Ray内核.zip 中的 config.json 文件
![image](https://github.com/user-attachments/assets/568bc88c-4fc8-4d23-92d0-2a72ddc6ea5d)

# 小功能优化
自适应调整列宽  
键盘 U, T, D, B键 移动节点排序  
订阅项 是否base64解码  
上下键选择节点时, 二维码同步刷新  
激活节点时, 滚动到显示节点  
订阅更新后, 滚动到显示节点  
批量测试时, 保存测试用的json配置文件  
克隆服务器后选中  
清除所选测试结果  
如果是 Vmess 协议, 使用 V2Ray (因为Xray只支持 AEAD 之后的Vmess, 不支持 "古典" Vmess)  
新建节点, 编辑节点后, 列表显示操作过的节点  

# 演示视频
https://www.youtube.com/watch?v=MmGTy5-mlXg


# 打包v2rayN-VLESS-Core.zip时
v3.29.0.3打包的是V2Ray v4.32.1版本，支持VLESS和XTLS的最后一个版本  
v3.29.0.4打包的是Xray v1.8.4版本  
v3.29.0.7打包的是Xray v1.8.6版本  
v3.29.0.8打包的是Xray v1.8.10版本  
v3.29.0.11打包的是Xray v1.8.24 和 V2Ray v5.21.0  

因为go v1.21以后不支持Windows7 系统，所以要么降级 [Xray v1.8.4](https://github.com/XTLS/Xray-core/releases/tag/v1.8.4)  
要么去下载对应的特殊版本的core，比如写了 Win7 的 https://github.com/XTLS/Xray-core/releases/tag/v1.8.24  

# 本项目会永远保持 .NET Framework 4.8
https://learn.microsoft.com/zh-CN/lifecycle/faq/dotnet-framework
![image](https://github.com/crazypeace/v2rayN-3.29-VLESS/assets/665889/8efc502f-c216-4091-b111-7f127cfff79f)

# 32位用户可以自己编译v2rayN
就是下载源码, 安装VS, 再点一下编译按钮  
https://zelikk.blogspot.com/2022/07/v2rayn-vless-v329.html

# 32位用户可以自己编译内核
就是下载源码, 安装Go, 设置命令行http代理, 再执行编译命令  
https://zelikk.blogspot.com/2024/07/win7-win10-32-64-xray-184.html  
