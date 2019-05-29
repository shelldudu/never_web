using Microsoft.Owin;
using Never.Web.Encryptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Never.WebApi.Owin.MessageHandlers
{
    /// <summary>
    /// 对内容流进行授权阅读
    /// </summary>
    public class DelegatingStream : Stream
    {
        #region ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="DelegatingStream"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="encryption">The encryption.</param>
        public DelegatingStream(IOwinContext context, IContentEncryptor encryption)
        {
            this.encryption = encryption;
            this.context = context;
            this.stream = context.Response.Body;
        }

        #endregion ctor

        #region field

        private readonly Stream stream = null;
        private readonly IOwinContext context;
        private readonly IContentEncryptor encryption = null;

        #endregion field

        #region stream

        /// <summary>
        ///
        /// </summary>
        public override bool CanRead
        {
            get
            {
                return this.stream.CanRead;
            }
        }

        /// <summary>
        ///
        /// </summary>
        public override bool CanSeek
        {
            get
            {
                return this.stream.CanSeek;
            }
        }

        /// <summary>
        ///
        /// </summary>
        public override bool CanWrite
        {
            get
            {
                return this.stream.CanWrite;
            }
        }

        /// <summary>
        ///
        /// </summary>
        public override long Length
        {
            get
            {
                return this.stream.Length;
            }
        }

        /// <summary>
        ///
        /// </summary>
        public override long Position
        {
            get
            {
                return this.stream.Position;
            }
            set
            {
                this.stream.Position = value;
            }
        }

        /// <summary>
        ///
        /// </summary>
        public override void Flush()
        {
            this.stream.Flush();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        public override long Seek(long offset, SeekOrigin origin)
        {
            return this.stream.Seek(offset, origin);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="value"></param>
        public override void SetLength(long value)
        {
            this.stream.SetLength(value);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public override int Read(byte[] buffer, int offset, int count)
        {
            return this.stream.Read(buffer, offset, count);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="offset"></param>
        /// <param name="count"></param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            if (offset == 0)
            {
                var @byte = this.encryption.Encrypt(buffer);
                this.stream.Write(@byte, 0, @byte.Length);
                return;
            }
            else
            {
                var @byte = new byte[count];
                Array.Copy(buffer, offset, @byte, 0, count);
                @byte = this.encryption.Encrypt(@byte);
                this.stream.Write(@byte, 0, @byte.Length);
                return;
            }

            //var text = Encoding.UTF8.GetString(buffer);
            //var data = Encoding.UTF8.GetBytes(encryption.Encrypt(text));
            //this.stream.Write(data, 0, data.Length);
        }

        #endregion stream
    }
}