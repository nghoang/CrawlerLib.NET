using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using HtmlAgilityPack;

namespace CrawlerLib.Net
{
    public class ParserLib
    {
        public static List<GeneralBlock> GetBlock(string content, string tag, string id, string name, string cls, string exName, string exValue, string value = "value")
        {
            HtmlDocument doc = new HtmlDocument();
            doc.OptionWriteEmptyNodes = true;
            doc.LoadHtml(content);

            string condition = "";

            if (id != "")
                condition += "@id='" + id + "'";

            if (name != "" && condition == "")
                condition += "@name='" + name + "'";
            else if (name != "")
                condition += " and @name='" + name + "'";

            if (cls != "" && condition == "")
                condition += "@class='" + cls + "'";
            else if (cls != "")
                condition += " and @class='" + cls + "'";

            if (exName != "" && condition == "")
                condition += "@" + exName + "='" + exValue + "'";
            else if (exValue != "")
                condition += " and @" + exName + "='" + exValue + "'";

            if (condition != "")
                condition = "[" + condition + "]";

            List<GeneralBlock> blocks = new List<GeneralBlock>();
            HtmlNodeCollection nodes = doc.DocumentNode.SelectNodes("//" + tag + condition);
            if (nodes == null)
                return blocks;

            foreach (HtmlNode node in nodes)
            {
                GeneralBlock b = new GeneralBlock();
                b.cls = node.GetAttributeValue("class", "");
                b.id = node.GetAttributeValue("id", "");
                //if (node.InnerHtml == "")
                //{
                //    b.innerHtml = node.ParentNode.InnerHtml;
                //    b.innerText = node.ParentNode.InnerText;
                //}
                //else
                //{
                    b.innerHtml = node.OuterHtml;
                    b.innerText = node.InnerText;
                //}
                b.name = node.GetAttributeValue("name", "");
                b.value = node.GetAttributeValue(value, ""); ;
                blocks.Add(b);
            }
            return blocks;
        }

        public static GeneralBlock GetBlockSingle(string content, string tag, string id, string name, string cls, string exName, string exValue, string value = "value", int AtPos = 0)
        {
            List<GeneralBlock> bs = GetBlock(content, tag, id, name, cls, exName, exValue, value);
            if (bs.Count > 0)
                return bs[AtPos];
            return null;
        }
    }
}
