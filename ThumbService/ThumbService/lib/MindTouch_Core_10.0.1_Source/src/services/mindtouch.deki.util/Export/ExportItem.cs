/*
 * MindTouch Core - open source enterprise collaborative networking
 * Copyright (c) 2006-2010 MindTouch Inc.
 * www.mindtouch.com  oss@mindtouch.com
 *
 * For community documentation and downloads visit wiki.developer.mindtouch.com;
 * please review the licensing section.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.IO;

using MindTouch.Xml;

namespace MindTouch.Deki.Export {
    public class ExportItem :IDisposable {
        public string DataId;
        public readonly Stream Data;
        public readonly long DataLength;
        public readonly XDoc ItemManifest;

        public ExportItem(string dataId, Stream data, long length, XDoc itemManifest) {
            DataId = dataId;
            Data = data;
            DataLength = length;
            ItemManifest = itemManifest;
        }

        public void Dispose() {
            Data.Dispose();
        }
    }
}
