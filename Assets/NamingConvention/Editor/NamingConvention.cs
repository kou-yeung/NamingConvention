#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;
using Ikriv.Xml;
using System.Xml.Serialization;
using System.Text.RegularExpressions;

namespace NamingConvention
{
    [InitializeOnLoad]
    public class WhatIsThis
    {
        // パターンファイルのパス
        public static readonly string SettingsFilePath = Path.Combine(Application.dataPath, "NamingConvention/Pattern.xml").Replace("\\", "/");

        static Detail.Patterns patterns;
        static Detail.Patterns Patterns
        {
            get
            {
                if(patterns == null)
                {
                    var serializer = new XmlSerializer(typeof(Detail.Patterns), GetOverrides());
                    using (var stream = new FileStream(SettingsFilePath, FileMode.Open))
                    {
                        patterns = (Detail.Patterns)serializer.Deserialize(stream);
                    }
                }
                return patterns;
            }
        }
        static GUIStyle style;
        static GUIStyle Style
        {
            get
            {
                if (style == null)
                {
                    style = new GUIStyle(GUI.skin.label);
                    style.normal.textColor = Color.yellow;
                }
                return style;
            }
        }

        static WhatIsThis()
        {
            EditorApplication.update += Update;

            EditorApplication.projectWindowItemOnGUI += (string guid, Rect selectionRect) =>
            {
                if (!String.IsNullOrEmpty(showInfo) && guid == currentGUID)
                {
                    var pos = selectionRect.position;
                    var size = selectionRect.size;

                    // 選択された文字列の幅分ずらす (+20 はアイコン幅)
                    var offsetX = (int)GUI.skin.label.CalcSize(new GUIContent(baseFn)).x + 20;
                    GUILayout.BeginArea(new Rect(pos.x + offsetX, pos.y, size.x, size.y));
                    GUILayout.Label(String.Format("( {0} )", showInfo), Style);
                    GUILayout.EndArea();
                }
            };
        }

        static string currentGUID;
        static string showInfo;
        static string baseFn;   // 選択されたものの名前

        static void Update()
        {
            if (Selection.assetGUIDs.Length != 1)
            {
                // 未選択 / 複数選択された場合、処理しない
                showInfo = null;
                currentGUID = null;
                return;
            }
            if (currentGUID == Selection.assetGUIDs.First()) return;    // 既に選択中の場合処理しない

            // 更新する
            currentGUID = Selection.assetGUIDs.First();

            // 情報取得
            showInfo = GetInfo(AssetDatabase.GUIDToAssetPath(currentGUID));
            //Detail.Utilits.ShowNotification(showInfo);
        }

        // パスを指定してい情報文字列を返す
        static string GetInfo(string path)
        {
            // 拡張子があれば削除する
            if(Path.HasExtension(path))
            {
                path = path.Replace(Path.GetExtension(path), "");
            }

            baseFn = Path.GetFileName(path);

            var res = "";

            foreach (var pattern in Patterns.patterns)
            {
                var match = pattern.Match(path);
                if (match == Match.Empty) continue;

                // ヒットしたグループを順番で処理する
                for(var i = 1; i < match.Groups.Count; ++i)
                {
                    var value = match.Groups[i].Value;
                    var find = Array.Find(pattern.groups, group => group.index == i && group.equal == value);
                    // 一致した場合、設定された文字列、そうでない場合、ヒットした文字列を入れます
                    res += (find != null) ? find.value : value;
                }
            }
            return res;
        }

        public static void Refresh()
        {
            patterns = null;
        }

        // XML のパース方法を定義する
        static XmlAttributeOverrides GetOverrides()
        {
            return new OverrideXml()
                .Override<Detail.Patterns>()
                    .XmlRoot("Patterns")
                    .Member("patterns").XmlElement("Pattern")
                .Override<Detail.Pattern>()
                    .Member("match").XmlAttribute("match")
                    .Member("groups").XmlElement("Group")
                .Override<Detail.Group>()
                    .Member("index").XmlAttribute("index")
                    .Member("equal").XmlAttribute("equal")
                    .Member("value").XmlText()
                .Commit();
        }
    }

    // パターンファイルが更新されたら、再読み込みします
    public class FolderInfoImport : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (var fn in importedAssets)
            {
                var fullpath = Path.GetFullPath(fn).Replace("\\", "/");
                if (fullpath == WhatIsThis.SettingsFilePath)
                {
                    WhatIsThis.Refresh();
                }
            }
        }
    }
}

#endif