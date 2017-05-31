/*
 * Smuxi - Smart MUltipleXed Irc
 *
 * Copyright (c) 2008, 2012, 2017 Mirco Bauer <meebey@meebey.net>
 *
 * Full GPL License: <http://www.gnu.org/licenses/gpl.txt>
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307 USA
 */

using System;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Smuxi.Common;

namespace Smuxi.Engine
{
    [Serializable]
    [DataContract]
    public class ImageMessagePartModel : MessagePartModel
    {
#if LOG4NET
        private static readonly log4net.ILog _Logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
        private string f_ImageFileName;
        private string f_AlternativeText;

        [DataMember]
        public override string Type {
            get {
                return "Image";
            }
        }

        [DataMember]
        public string ImageFileName {
            get {
                return f_ImageFileName;
            }
            set {
                f_ImageFileName = value;
            }
        }
        
        [DataMember]
        public string AlternativeText {
            get {
                return f_AlternativeText;
            }
            set {
                f_AlternativeText = value;
            }
        }
        
        public ImageMessagePartModel()
        {
        }

        public ImageMessagePartModel(string imageFileName, string alternativeText)
        {
            if (imageFileName == null) {
                throw new ArgumentNullException("imageFileName");
            }
            
            f_ImageFileName = imageFileName;
            f_AlternativeText = alternativeText;
        }
        
        public ImageMessagePartModel(string imageFileName) : this(imageFileName, null)
        {
        }

        public override string ToString()
        {
            return AlternativeText;
        }

        protected ImageMessagePartModel(SerializationInfo info, StreamingContext ctx) :
                                   base(info, ctx)
        {
        }
        
        protected override void SetObjectData(SerializationReader sr)
        {
            base.SetObjectData(sr);
            
            f_ImageFileName   = sr.ReadString();
            f_AlternativeText = sr.ReadString();
        }
        
        protected override void GetObjectData(SerializationWriter sw)
        {
            base.GetObjectData(sw);

            sw.Write( f_ImageFileName);
            sw.Write( f_AlternativeText);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is ImageMessagePartModel)) {
                return false;
            }

            var textPart = (ImageMessagePartModel) obj;
            return Equals(textPart);
        }

        public override bool Equals(MessagePartModel part)
        {
            var imagePart = part as ImageMessagePartModel;
            if ((object) imagePart == null) {
                return false;
            }

            if (f_ImageFileName != imagePart.ImageFileName) {
                return false;
            }
            if (f_AlternativeText != imagePart.AlternativeText) {
                return false;
            }

            return base.Equals(imagePart);
        }
    }
}
