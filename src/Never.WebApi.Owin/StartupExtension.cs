using Never.WebApi.Owin.MessageHandlers;
using Microsoft.Owin;
using Owin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dispatcher;
using Never.Web.Encryptions;

namespace Never.WebApi.Owin
{
    /// <summary>
    /// 启动扩展
    /// </summary>
    public static class StartupExtension
    {
        /// <summary>
        /// The get method dictionary
        /// </summary>
        private static readonly IDictionary<string, Regex> getMethodDict = null;

        #region ctor

        static StartupExtension()
        {
            getMethodDict = new System.Collections.Concurrent.ConcurrentDictionary<string, Regex>(4 * System.Environment.ProcessorCount, 100);
        }

        #endregion ctor

        #region api

        /// <summary>
        /// 启用api对数据解密
        /// </summary>
        /// <param name="appBuilder"></param>
        /// <param name="configuration">The configuration.</param>
        /// <param name="findSecurityModelCallback">指示如何在当前请求中找到相应的加密与解密的方法</param>
        /// <returns></returns>
        public static IAppBuilder UseOwinApiHttpSecurityHandler(this IAppBuilder appBuilder, HttpConfiguration configuration, Func<IOwinRequest, IContentEncryptor> findSecurityModelCallback)
        {
            if (configuration.Properties.ContainsKey("UseOwinApiHttpSecurityHandler"))
                return appBuilder;

            appBuilder.Use((c, x) =>
            {
                var encryption = findSecurityModelCallback == null ? null : findSecurityModelCallback(c.Request);
                if (encryption == null)
                    return x.Invoke();

                if ((encryption as UnAuthorizeContentEncryptor) != null)
                {
                    var task = new TaskCompletionSource<HttpResponseMessage>();
                    c.Set<int>("owin.ResponseStatusCode", 401);
                    return c.Response.WriteAsync("Unauthorized");
                }

                /*uri*/
                RewriteUri(c, encryption);

                /*content*/
                RewriteContent(c, encryption);

                return x.Invoke();
            });

            configuration.Properties["UseOwinApiHttpSecurityHandler"] = "t";
            return appBuilder;
        }

        /// <summary>
        /// 重写Uri
        /// </summary>
        /// <param name="context">The request.</param>
        /// <param name="encryption">解密的model</param>
        private static void RewriteUri(IOwinContext context, IContentEncryptor encryption)
        {
            Regex reg = null;
            if (!getMethodDict.TryGetValue(encryption.ParamKey, out reg))
            {
                reg = new Regex(string.Format("{0}=(?<security>[^&]*)", encryption.ParamKey), RegexOptions.Compiled | RegexOptions.IgnoreCase);
                getMethodDict[encryption.ParamKey] = reg;
            }

            context.Request.QueryString = new QueryString(reg.Replace(context.Request.QueryString.Value, o =>
            {
                return encryption.Decrypt(o.Groups["security"].Value);
            }));
        }

        /// <summary>
        /// 重写content
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="encryption">解密的model</param>
        private static void RewriteContent(IOwinContext context, IContentEncryptor encryption)
        {
            /*输出加密的内容*/
            var @byte = default(byte[]);
            using (var memoryStream = new MemoryStream())
            {
                context.Request.Body.CopyTo(memoryStream);
                @byte = memoryStream.ToArray();
            }

            context.Request.Body = new MemoryStream(encryption.Decrypt(@byte));

            //using (var reader = new StreamReader(context.Request.Body))
            //{
            //    var data = encryption.Decrypt(reader.ReadToEnd());
            //    context.Request.Body = new MemoryStream(Encoding.UTF8.GetBytes(data));
            //}

            context.Response.Body = new DelegatingStream(context, encryption);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static Task CreateErrorResponse(this IOwinContext context)
        {
            return CreateErrorResponse(context, HttpStatusCode.Unauthorized, "Unauthorized");
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="context"></param>
        /// <param name="statusCode"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static Task CreateErrorResponse(this IOwinContext context, HttpStatusCode statusCode, string message)
        {
            context.Set<int>("owin.ResponseStatusCode", (int)statusCode);
            return context.Response.WriteAsync(message);
        }

        /// <summary>
        /// 获取Ip
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static string GetIP(this IOwinContext context)
        {
            var ip = context.Request.Headers["X-Forwarded-For"] ?? context.Request.Headers["X-Real-Ip"] ?? context.Request.RemoteIpAddress;
            return ip.IsIP() ? ip : string.Empty;
        }

        #endregion api
    }
}