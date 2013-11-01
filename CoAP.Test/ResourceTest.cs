using System;
using System.Collections.Generic;
using CoAP.EndPoint.Resources;

namespace CoAP.Test
{
    class ResourceTest
    {
        public void SimpleTest()
        {
            String input = "</sensors/temp>;ct=41;rt=\"TemperatureC\"";
            Resource root = RemoteResource.NewRoot(input);

            Resource res = root.GetResource("/sensors/temp");
            Assert.IsNotNull(res);
            Assert.IsEqualTo(res.Name, "temp");
            Assert.IsEqualTo(41, res.ContentTypeCode);
            Assert.IsEqualTo("TemperatureC", res.ResourceType);
        }

        public void ExtendedTest()
        {
            String input = "</my/Päth>;rt=\"MyName\";if=\"/someRef/path\";ct=42;obs;sz=10";
            Resource root = RemoteResource.NewRoot(input);

            RemoteResource my = new RemoteResource("my");
            my.ResourceType = "replacement";
            root.AddSubResource(my);

            Resource res = root.GetResource("/my/Päth");
            Assert.IsNotNull(res);
            res = root.GetResource("my/Päth");
            Assert.IsNotNull(res);
            res = root.GetResource("my");
            res = res.GetResource("Päth");
            Assert.IsNotNull(res);
            res = res.GetResource("/my/Päth");
            Assert.IsNotNull(res);

            Assert.IsEqualTo(res.Name, "Päth");
            Assert.IsEqualTo(res.Path, "/my/Päth");
            Assert.IsEqualTo(res.ResourceType, "MyName");
            Assert.IsEqualTo(res.InterfaceDescriptions[0], "/someRef/path");
            Assert.IsEqualTo(42, res.ContentTypeCode);
            Assert.IsEqualTo(10, res.MaximumSizeEstimate);
            Assert.IsEqualTo(true, res.Observable);

            res = root.GetResource("my");
            Assert.IsNotNull(res);
            Assert.IsEqualTo("replacement", res.ResourceTypes[0]);
        }

        public void ConversionTest()
        {
            String link1 = "</myUri/something>;ct=42;if=\"/someRef/path\";obs;rt=\"MyName\";sz=10";
            String link2 = "</myUri>;rt=\"NonDefault\"";
            String link3 = "</a>";
            String format = link1 + "," + link2 + "," + link3;
            Resource res = RemoteResource.NewRoot(format);
            String result = LinkFormat.Serialize(res, null, true);
            Assert.IsEqualTo(link3 + "," + link2 + "," + link1, result);
        }

        public void ConcreteTest()
        {
            String link = "</careless>;rt=\"SepararateResponseTester\";title=\"This resource will ACK anything, but never send a separate response\",</feedback>;rt=\"FeedbackMailSender\";title=\"POST feedback using mail\",</helloWorld>;rt=\"HelloWorldDisplayer\";title=\"GET a friendly greeting!\",</image>;ct=21;ct=22;ct=23;ct=24;rt=\"Image\";sz=18029;title=\"GET an image with different content-types\",</large>;rt=\"block\";title=\"Large resource\",</large_update>;rt=\"block\";rt=\"observe\";title=\"Large resource that can be updated using PUT method\",</mirror>;rt=\"RequestMirroring\";title=\"POST request to receive it back as echo\",</obs>;obs;rt=\"observe\";title=\"Observable resource which changes every 5 seconds\",</query>;title=\"Resource accepting query parameters\",</seg1/seg2/seg3>;title=\"Long path resource\",</separate>;title=\"Resource which cannot be served immediately and which cannot be acknowledged in a piggy-backed way\",</storage>;obs;rt=\"Storage\";title=\"PUT your data here or POST new resources!\",</test>;title=\"Default test resource\",</timeResource>;rt=\"CurrentTime\";title=\"GET the current time\",</toUpper>;rt=\"UppercaseConverter\";title=\"POST text here to convert it to uppercase\",</weatherResource>;rt=\"ZurichWeather\";title=\"GET the current weather in zurich\"";
            Resource res = RemoteResource.NewRoot(link);
            String result = LinkFormat.Serialize(res, null, true);
            Assert.IsEqualTo(link, result);
        }

        public void MatchTest()
        {
            String link1 = "</myUri/something>;ct=42;if=\"/someRef/path\";obs;rt=\"MyName\";sz=10";
            String link2 = "</myUri>;ct=50;rt=\"MyName\"";
            String link3 = "</a>;sz=10;rt=\"MyNope\"";
            String format = link1 + "," + link2 + "," + link3;
            Resource res = RemoteResource.NewRoot(format);

            List<Option> query = new List<Option>();
            query.Add(Option.Create(OptionType.UriQuery, "rt=MyName"));

            String queried = LinkFormat.Serialize(res, query, true);
            Assert.IsEqualTo(link2 + "," + link1, queried);
        }
    }
}
