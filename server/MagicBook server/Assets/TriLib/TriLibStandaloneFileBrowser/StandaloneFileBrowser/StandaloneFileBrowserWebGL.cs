﻿#pragma warning disable 618
#if UNITY_WEBGL && !UNITY_EDITOR

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace TriLibCore.SFB
{
    [Serializable]
    public class JsonFilesWrapper {
        public JsonFile[] files;
    }
    [Serializable]
    public class JsonFile {
        public string url;
        public string name;
    }
    public class StandloneFileBrowserWebGLHelper : MonoBehaviour
    {
        public Action<IList<ItemWithStream>> MultipleFilesCallback;
        public Action<ItemWithStream> SingleFileCallback;

        private IEnumerator InvokeCallback(string json)
        {
            var filesWrapper = JsonUtility.FromJson<JsonFilesWrapper>($"{{\"files\":{json}}}");
            var browserFiles = filesWrapper.files;
			var browserItemsWithStream = new ItemWithStream[browserFiles.Length];
            if (browserFiles.Length > 0)
            {
                for (var i = 0; i < browserFiles.Length; i++)
                {
                    var browserFile = browserFiles[i];
                    var loader = new WWW(browserFile.url);
                    yield return loader;
                    if (string.IsNullOrWhiteSpace(loader.error))
                    {
                        browserItemsWithStream[i] = new ItemWithStream
                        {
                            Name = browserFile.name.ToString(),
                            Stream = new MemoryStream(loader.bytes, 0, loader.bytes.Length, true, true)
                        };
                    }
                    else
                    {
                        throw new Exception(loader.error);
                    }
                }
			}
            if (MultipleFilesCallback != null) {
			    MultipleFilesCallback.Invoke(browserItemsWithStream);
            } else if (SingleFileCallback != null && browserItemsWithStream.Length > 0) {
                SingleFileCallback.Invoke(browserItemsWithStream[0]);
            }
            SingleFileCallback = null;
            MultipleFilesCallback = null;
            Destroy(gameObject);
        }
    }

    public class StandaloneFileBrowserWebGL : IStandaloneFileBrowser<ItemWithStream>
    {

        [DllImport("__Internal")]
        private static extern void UploadFile(string gameObjectName, string methodName, string filter, bool multiple, bool selectDirectory);

        [DllImport("__Internal")]
        private static extern void DownloadFile(string gameObjectName, string methodName, string filename, byte[] byteArray, int byteArraySize);

        private bool _processing;

        public byte[] Data;

        public StandaloneFileBrowserWebGL()
        {
        }

        public IList<ItemWithStream> OpenFilePanel(string title, string directory, ExtensionFilter[] extensions, bool multiselect)
        {
            throw new NotSupportedException();
        }

        public IList<ItemWithStream> OpenFolderPanel(string title, string directory, bool multiselect)
        {
            throw new NotSupportedException();
        }

        public ItemWithStream SaveFilePanel(string title, string directory, string defaultName, ExtensionFilter[] extensions)
        {
            throw new NotSupportedException();
        }

        public void OpenFilePanelAsync(string title, string directory, ExtensionFilter[] extensions, bool multiselect, Action<IList<ItemWithStream>> cb)
        {
            var helper = new GameObject(Guid.NewGuid().ToString()).AddComponent<StandloneFileBrowserWebGLHelper>();
            helper.MultipleFilesCallback = cb;
            UploadFile(helper.name, "InvokeCallback", GetFilterFromFileExtensionList(extensions), multiselect, false);
        }

        public void OpenFolderPanelAsync(string title, string directory, bool multiselect, Action<IList<ItemWithStream>> cb)
        {
            var helper = new GameObject(Guid.NewGuid().ToString()).AddComponent<StandloneFileBrowserWebGLHelper>();
            helper.MultipleFilesCallback = cb;
            UploadFile(helper.name, "InvokeCallback", null, false, true);
        }

        public void SaveFilePanelAsync(string title, string directory, string defaultName, ExtensionFilter[] extensions, Action<ItemWithStream> cb)
        {
            if (Data == null)
            {
                return;
            }
            var helper = new GameObject(Guid.NewGuid().ToString()).AddComponent<StandloneFileBrowserWebGLHelper>();
            helper.SingleFileCallback = cb;
            DownloadFile(helper.name, "InvokeCallback", defaultName, Data, Data.Length);
        }

        private static string GetFilterFromFileExtensionList(ExtensionFilter[] extensions)
        {
            var filterString = "";
            var addedFormats = new List<string>();
            if (extensions != null)
            {
                foreach (var extension in extensions)
                {
                    foreach (var format in extension.Extensions)
                    {
                        if (format == "*.*" || format == ".*" || format == "*") {
                            continue;
                        }
                        if (filterString != "")
                        {
                            filterString += ", ";
                        }
                        if (!addedFormats.Contains(format)) {
                            filterString += "." + (format[0] == '.' ? format.Substring(1) : format);
                            addedFormats.Add(format);
                        }
                    }
                }
            }
            return filterString;
        }
    }
}
#endif