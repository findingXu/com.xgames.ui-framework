using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace XGames.UIFramework
{
    public class UIContextPrinter
    {
        public static string GenerateCode(UIContext uiContext)
        {
            var fileExt = Path.GetExtension(uiContext.bindFile);
            return fileExt switch
            {
                ".ts" => GenerateTsCode(uiContext),
                _ => throw new NotImplementedException()
            };
        }
        
        private static string GetComponentTypeName(Component component)
        {
            var typeName = "";
            if (component is UIContext context)
            {
                if(File.Exists(context.bindFile))
                {
                    var tsCode = File.ReadAllText(context.bindFile);
                    var match = Regex.Match(tsCode, @$"class\s+(\w+)\s+extends\s+({UIEditorConfig.ClassUIBaseName})");
                    // TODO: 匹配同文件多个类名
                    if (match.Success)
                    {
                        typeName = match.Groups[1].Value;
                    }
                }
            }

            if (typeName.Length == 0)
            {
                var type = component.GetType();
                typeName = $"CS.{type.FullName}";
            }

            return typeName;
        }

        private static string GenerateTsCode(UIContext uiContext)
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            // Fields
            foreach (var node in uiContext.lstComponentsInEditor)
            {
                var fieldName = node.fieldName;
                var fieldType = GetComponentTypeName(node.component);
                sb.AppendLine($"\t{fieldName}!: {fieldType};");
            }
            sb.AppendLine();
            
            // GetPrefabPath
            sb.AppendLine($"\tpublic {UIEditorConfig.FuncGetPath}(): string {{");
            sb.AppendLine($"\t\treturn \"{uiContext.prefabPath}\";");
            sb.AppendLine("\t}");
            sb.AppendLine();

            // BindComponent
            sb.AppendLine($"\tprotected {UIEditorConfig.FuncBindComponents}(): void {{");
            for (int i = 0, cnt = uiContext.lstComponentsInEditor.Count; i < cnt; ++i)
            {
                var node = uiContext.lstComponentsInEditor[i];

                var fieldName = node.fieldName;
                var fieldType = GetComponentTypeName(node.component);
                sb.AppendLine($"\t\tthis.{fieldName} = this.{UIEditorConfig.FuncGetComponent}<{fieldType}>({i});");
            }
            sb.AppendLine("\t}");
            sb.Append("\t");

            var code = string.Format(UIEditorConfig.BindHashTag, uiContext.bindHash, sb);
            return code;
        }

        private static string GenerateCsCode(UIContext uiContext)
        {
            var sb = new StringBuilder();
            sb.AppendLine();
            // Fields
            foreach (var node in uiContext.lstComponentsInEditor)
            {
                var fieldName = node.fieldName;
                var fieldType = GetComponentTypeName(node.component);
                sb.AppendLine($"\t{fieldType.Split(".").Last()} {fieldName};");
            }
            
            // GetPrefabPath
            sb.AppendLine($"\tpublic override string {UIEditorConfig.FuncGetPath}()");
            sb.AppendLine("\t{");
            sb.AppendLine($"\t\treturn \"{uiContext.prefabPath}\";");
            sb.AppendLine("\t}");
            
            // BindComponent
            sb.AppendLine($"\tprotected override void {UIEditorConfig.FuncBindComponents}()");
            sb.AppendLine("\t{");
            for (int i = 0, cnt = uiContext.lstComponentsInEditor.Count; i < cnt; ++i)
            {
                var node = uiContext.lstComponentsInEditor[i];

                var fieldName = node.fieldName;
                var fieldType = GetComponentTypeName(node.component);
                var funGetField = node.component is UIContext ? UIEditorConfig.FuncGetSubControl : UIEditorConfig.FuncGetComponent;
                sb.AppendLine($"\t\tthis.{fieldName} = this.{funGetField}<{fieldType.Split(".").Last()}>({i});");
            }
            sb.AppendLine("\t}");
            sb.Append("\t");

            var code = string.Format(UIEditorConfig.BindHashTag, uiContext.bindHash, sb);
            return code;
        }
    }
}