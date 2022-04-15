# 背景

这是一个基于 .net 6 + [YAPR](https://microsoft.github.io/) 的远程调用日志记录器，目的是用于解决各个服务、模块间日志行为不统一的问题。通过增加一层代理后，以统一的方式集中来记录请求-响应的交互过程数据。

# 功能说明

- [X] 灵活的配置方式
- [X] 支持 GB2312 等字符集[^1]
- [X] 支持 Brotli / GZip / Deflate 报文解码
- [X] 支持 NACOS 动态配置
- [X] 支持阿里云日志服务
- [ ] 内网穿透 

# 相关依赖

* [Yarp.ReverseProxy](https://www.nuget.org/packages/Yarp.ReverseProxy/)
* [nacos-sdk-csharp](https://www.nuget.org/packages/nacos-sdk-csharp/) 
* [nacos-sdk-csharp.Extensions.Configuration](https://www.nuget.org/packages/nacos-sdk-csharp.Extensions.Configuration/)
* [Aliyun.Api.LogService](https://www.nuget.org/packages/Aliyun.Api.LogService/)

# 配置说明

配置文件使用 JSON 结构，除 .net 默认节点外，还包含以下主要节点：
```json
{
    "NACOS" : {NACOS},
    "LogStore" : {LogStore},
    "YARP" : {YARP}    
}
```

## NACOS

具体可参考 [官方文档](https://nacos-sdk-csharp.readthedocs.io/) 进行配置，这里以 [阿里云微服务引擎](https://www.aliyun.com/product/aliware/mse) 做为示例：

```json
{
    "ServerAddresses" : [
      "http://mse-***-nacos-ans.mse.aliyuncs.com:8848/"
    ],
    "EndPoint" : "mse.cn-***.aliyuncs.com",
    "AccessKey" : "******",
    "SecretKey" : "******",
    "Namespace" : "{Guid}",
    "ConfigUseRpc" : true,
    "Listeners" : [
      {        
        "DataId" : "{DataId}",
        "Group" : "{Group}",
        "Optional": true
      }
    ]
}
```

> ```ServerAddresses``` : [ string ]

实例地址与端口，可在具体实例的【基础信息】页签中找到。

> ```EndPoint``` : string

接入点地址，可在 [服务区域列表](https://next.api.aliyun.com/product/mse#endpoint) 中找到。

> ```AccessKey``` : string

访问密钥组之一，可以参照 [访问控制](https://help.aliyun.com/document_detail/175967.html) 文档获取。

> ```SecretKey``` : string

同上。

> ```Namespace``` : string

可在具体实例的【命名空间】页签中的列表找到，填入【命名空间ID】的那列的值，不要填写【命名空间名】列的值。

> ```ConfigUseRpc``` : boolean [ ```true``` | ```false``` ]

是否使用 gRPC 协议与服务端对接[^2]，服务端是 2.0.0 的，一定要设置成 ```true``` 。

> ```Listeners``` : [ object ]

### Listeners

监听配置，可以同时监听多组配置，实现热重载。在具体实例的【配置管理】=>【配置列表】页签中找到。

> ```DataId``` : string

对应【Data ID】列，字符串型。

> ```Group``` : string

对应【Group】列，字符串型。

> ```Optional``` : boolean [ ```true``` | ```false``` ]

## LogStore

日志输出的相关配置，可定义多个输出同时记录日志数据。若想自己动手添加实现 [IRpcLogProvider](https://github.com/sduo/RPC/blob/bb32fdc7a4292747b601dcca6ad374416948ccab/RPC/Logger/Provider/IRpcLogProvider.cs) 接口后，在 [RpcLogStore](https://github.com/sduo/RPC/blob/bb32fdc7a4292747b601dcca6ad374416948ccab/RPC/Logger/RpcLogStore.cs#L17) 注册即可。

```json
{
    "{StoreId}":{
        "Type":"{Type}",
        "Configuration":{Configuration}
    }
}
```

> ```StoreId``` : string

名称随意，保持唯一即可。

> ```Type``` : string [ ```Console``` | ```AliCloudSLS``` ]

日志输出类型，目前支持两种，可参考 [RpcLogProvider](https://github.com/sduo/RPC/blob/bb32fdc7a4292747b601dcca6ad374416948ccab/RPC/Logger/Provider/IRpcLogProvider.cs#L6) 枚举值。

### Configuration

当前日志输出的特定配置。

#### Console

无

#### AliCloudSLS

```json
{
    "Endpoint" : "cn-***.log.aliyuncs.com",
    "Project" : "{Project}",
    "Store" : "{Store}",
    "AK" : "******",
    "SK" : "******"
}
```

> ```EndPoint``` : string

接入点地址，可在 [服务区域列表](https://next.api.aliyun.com/product/Sls#endpoint) 中找到。

> ```Project``` : string

可在【日志服务】控制台首页的【Project 列表】中找到。

> ```Store``` : string

日志库，可在具体的 Project 下找到。

> ```AK``` : string（```AccessKey```）

访问密钥组之一，可以参照 [访问控制](https://help.aliyun.com/document_detail/175967.html) 文档获取。

> ```SK``` : string（```SecretKey```）

同上。

## YARP

YARP 的相关配置参考 [官方文档](https://microsoft.github.io/) 即可，下面重点说明日志记录的相关配置。

```json
{
    "CORS" : {CORS},
    "RPC" : {RPC}
}
```

### CORS

跨域相关配置，YAPR 部分的配置参考 [Cross-Origin Requests](https://microsoft.github.io/reverse-proxy/articles/cors.html) 文档，具体可参考 [PolicyExtensions](https://github.com/sduo/RPC/blob/bb32fdc7a4292747b601dcca6ad374416948ccab/RPC/CORS/PolicyExtensions.cs) 的实现。

```json
{
    "{CorsId}" : {
        "Origins" : [ "{Origin}" ],
        "Methods" : [ "{Method}" ],
        "Headers" : [ "{Header}" ],
        "ExposedHeaders" : [ "{Header}" ]
    }
}
```

> ```CorsId``` : string

规则标识号，供 YAPR 配置中 ```CorsPolicy``` 使用。

> ```Origins```: [ string ]

控制 [Access-Control-Allow-Origin](https://developer.mozilla.org/zh-CN/docs/Web/HTTP/CORS#access-control-allow-origin) 字段的值（默认：```AllowAnyOrigin```）。

> ```Methods```: [ string ]

控制 [Access-Control-Allow-Methods](https://developer.mozilla.org/zh-CN/docs/Web/HTTP/CORS#access-control-allow-methods) 字段的值（默认：```AllowAnyMethod```）。

> ```Headers```: [ string ]

控制 [Access-Control-Allow-Headers](https://developer.mozilla.org/zh-CN/docs/Web/HTTP/CORS#access-control-allow-headers) 字段的值（默认：```AllowAnyHeader```）。

> ```ExposedHeaders```: [ string ]

控制 [Access-Control-Expose-Headers](https://developer.mozilla.org/zh-CN/docs/Web/HTTP/CORS#access-control-expose-headers) 字段的值（默认：无）

### RPC

```json
{
    "{RpcId}":{
        "TraceId" : true,
        "ConnectionId" : true,
        "IpFamily" : false,
        "Ip" : true,
        "Host" : true,
        "Path" : true,
        "Scheme" : true,
        "Protocol" : true,
        "Method" : true,
        "RouteId" : true,
        "ClusterId" : true,
        "DestinationId" : true,
        "DestinationAddress" : true,
        "Request" : {Request},
        "Response" : {Response}
    }
}
```

> ```TraceId``` : boolean [ ```true``` | ```false``` ]

是否启用 ```Trace Id``` 字段，即 ```context.TraceIdentifier``` 的值。

> ```ConnectionId``` : boolean [ ```true``` | ```false``` ]

是否启用 ```Connection Id``` 字段，即 ```context.Connection.Id``` 的值。

> ```IpFamily``` : boolean [ ```true``` | ```false``` ]

是否启用 ```Ip Family``` 字段，即 ```context.Connection.RemoteIpAddress.AddressFamily``` 的值。

> ```Ip``` : boolean [ ```true``` | ```false``` ]

是否启用 ```Ip Address``` 字段，即 ```context.Connection.RemoteIpAddress``` 的值。

> ```Host``` : boolean [ ```true``` | ```false``` ]

是否启用 ```Host``` 字段，即 ```context.Request.Host``` 的值。

> ```Path``` : boolean [ ```true``` | ```false``` ]

是否启用 ```Path``` 字段，即 ```context.Request.Path``` 的值。

> ```Scheme``` : boolean [ ```true``` | ```false``` ]

是否启用 ```Scheme``` 字段，即 ```context.Request.Scheme``` 的值。

> ```Protocol``` : boolean [ ```true``` | ```false``` ]

是否启用 ```Protocol``` 字段，即 ```context.Request.Protocol``` 的值。

> ```Method``` : boolean [ ```true``` | ```false``` ]

是否启用 ```Method``` 字段，即 ```context.Request.Method``` 的值。

> ```RouteId``` : boolean [ ```true``` | ```false``` ]

是否启用 ```Route Id``` 字段，即 YARP 中 ```RouteId``` 的值。

> ```ClusterId``` : boolean [ ```true``` | ```false``` ]

是否启用 ```Cluster Id``` 字段，即 YARP 中 ```ClusterId``` 的值。

> ```DestinationId``` : boolean [ ```true``` | ```false``` ]

是否启用 ```Destination Id``` 字段，即 YARP 中 ```DestinationId``` 的值。

> ```DestinationAddress``` : boolean [ ```true``` | ```false``` ]

是否启用 ```Destination Address``` 字段，即 YARP 中 ```DestinationAddress``` 的值。

#### Request

```json
{
    "Query" : {Collection},
    "Header" : {Collection},
    "Body" : {
        "Form" : {
            {Collection},
            "File" : false
        },
        "Text" : {
            "Type" : [ "{Media Type}" ],
            "Charset" : "{Charset}"
        },
        "Raw" : {
            "Type" : [ "{Media Type}" ],
            "Base64" : false
        }
    }
}
```

> ```Query``` : {Collection}

用于配置查询字符串的相关记录行为。

* 若无该节点时，则不记录查询字符串；
* 若为空节点时，则原样记录查询字符串；

> ```Header``` : {Collection}

用于配置请求头的相关记录行为。

* 若无该节点时，则不记录请求头；
* 若为空节点时，则原样记录请求头；

> ```Body``` : object

用于配置请求正文的相关记录行为。

* 若无该节点时，则不记录请求正文；

> ```Form``` : {Collection}

用于配置表单型请求正文的相关记录行为。

* 若无该节点时，则不记录表单型请求正文；
* 若为空节点时，则原样记录表单型请求正文；

> ```File``` : boolean [ ```true``` | ```false``` ]

是否启用 ```Request File``` 字段（默认：```false```），若启用则会记录表单中文件的名称、文件名和文件大小。

> ```Text``` : object

用于配置以文本的方式记录请求正文的相关行为。

* 若无该节点时，则不以文本的方式记录请求正文；

> ```Type```: [ string ]

适用于以文本方式记录请求正文的媒体类型，如 ```text/plain```、```application/json```、```application/xml``` 等。

> ```Charset```:  string

当 ```Content-Type``` 中解析 ```Encoding``` 失败时，所使用的缺省 ```Encoding``` 名称。若制定的名称非法则会使用 ```Encoding.UTF8``` 作为最终值进行解析。

> ```Raw``` : object

用于配置以字节的方式记录请求正文的相关行为。

* 若无该节点时，则不以字节的方式记录请求正文；

> ```Type```: [ string ]

适用于以字节方式记录请求正文的媒体类型，若无该节点，则记录所有的媒体类型；若指定了该节点，则仅记录指定的媒体类型。

> ```Base64``` : boolean [ ```true``` | ```false``` ]

以字节方式记录的请求正文是否使用 ```Base64``` 的方式表示，默认使用 ```Hex``` 方式。

#### Response

```json
{
    "Code" : true,
    "Header" : {Collection},
    "Body" : {
        "Text" : {
            "Type" : [ "{Media Type}" ],
            "Charset" : "{Charset}",
            "Brotli" : true,
            "GZip" : true,
            "Deflate" : true 
        },
        "Raw" : {
            "Type" : [ "{Media Type}" ],
            "Base64" : false
        }
    }
}
```

> ```Code``` : boolean [ ```true``` | ```false``` ]

是否启用 ```Status Code``` 字段，即 ```context.Response.StatusCode``` 的值。

> ```Header``` : {Collection}

用于配置响应头的相关记录行为。

* 若无该节点时，则不记录响应头；
* 若为空节点时，则原样记录响应头；

> ```Body``` : object

用于配置响应正文的相关记录行为。

* 若无该节点时，则不记录响应正文；

> ```Text``` : object

用于配置以文本的方式记录响应正文的相关行为。

* 若无该节点时，则不以文本的方式记录响应正文；

> ```Type```: [ string ]

适用于以文本方式记录响应正文的媒体类型，如 ```text/plain```、```application/json```、```application/xml``` 等。

> ```Charset```:  string

当 ```Content-Type``` 中解析 ```Encoding``` 失败时，所使用的缺省 ```Encoding``` 名称。若制定的名称非法则会使用 ```Encoding.UTF8``` 作为最终值进行解析。

> ```Brotli``` : boolean [ ```true``` | ```false``` ]

是否自动解析 ```ContentEncoding``` 包含 ```br``` 的响应正文。


> ```GZip``` : boolean [ ```true``` | ```false``` ]

是否自动解析 ```ContentEncoding``` 包含 ```gzip``` 的响应正文。


> ```Deflate``` : boolean [ ```true``` | ```false``` ]

是否自动解析 ```ContentEncoding``` 包含 ```deflate``` 的响应正文。

> ```Raw``` : object

用于配置以字节的方式记录响应正文的相关行为。

* 若无该节点时，则不以字节的方式记录响应正文；

> ```Type```: [ string ]

适用于以字节方式记录响应正文的媒体类型，若无该节点，则记录所有的媒体类型；若指定了该节点，则仅记录指定的媒体类型。

> ```Base64``` : boolean [ ```true``` | ```false``` ]

以字节方式记录的响应正文是否使用 ```Base64``` 的方式表示，默认使用 ```Hex``` 方式。

#### Collection

```json
{
    "Block" : [ {Match} ],
    "Expose" : [ {Map} ],
    "Mask" : [ {Mask} ]
}
```

> ```Block``` : [ {Match} ]

不记录对应集合中匹配该规则的键值对。

> ```Expose``` : [ {Map} ]

将对应集合中匹配该规则的键值对暴露到上层日志结构中。

> ```Mask``` : [ {Mask} ]

将对应集合中匹配该规则的键值对进行掩码保护。


##### Match

用于从键值对集合中匹配制定键值对。

```json
{
    "Key" : "{key}",
    "IgnoreCase" : true
}
```

> ```Key``` : string

需要匹配的键名，必填。

> ```IgnoreCase``` : boolean [ ```true``` | ```false``` ]

匹配时是否忽略大小写（默认：true）。


##### Mask

用于给键值对的值进行掩码，继承于 ```Match``` 结构。

```json
{
    "Key":"{key}",
    "IgnoreCase":true,
    "Regex":"{Regex}",
    "Replacement":null,
    "Options":{RegexOptions}
}
```

> ```Regex``` : string

用来给值进行掩码操作的正则表达式，若为空时，则使用原样返回对应的值。

> ```Replacement``` : string

用来给值进行掩码操作的替换值，若为空时，则使用 ```*``` 返回对应的值。

> ```Options``` : [RegexOptions](https://docs.microsoft.com/zh-cn/dotnet/api/system.text.regularexpressions.regexoptions?view=net-6.0)

用来给值进行掩码操作的正则表达式选项（默认：```RegexOptions.IgnoreCase | RegexOptions.Singleline```）。

##### Map

用于给键值对进行映射，继承于 ```Mask``` 结构。

```json
{
    "Key":"{key}",                            
    "IgnoreCase":true,
    "Name":"{Name}",
    "OriginKey":true,
    "Regex":"{Regex}",
    "Replacement":null,
    "Options":{RegexOptions}
}
```

> ```Name``` : string

匹配键的映射名称，若为空时，按照 ```OriginKey``` 的配置执行对应行为。

> ```OriginKey``` : boolean [ ```true``` | ```false``` ]

是否返回原始键，若为 ```true``` 时，则返回匹配键值对的原始键；若为 ```false``` 时，则返回配置中的 ```Key``` 字段。

# 参考

> 1. [CodePagesEncodingProvider](https://docs.microsoft.com/zh-cn/dotnet/api/system.text.codepagesencodingprovider.instance)
> 2. [聊一聊如何在 .NET Core 中使用 Nacos 2.0](https://zhuanlan.zhihu.com/p/358860190)
