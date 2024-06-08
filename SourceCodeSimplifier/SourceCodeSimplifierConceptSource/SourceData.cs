namespace SourceCodeSimplifierConceptSource
{
    public class SomeInnerData
    {
        public SomeInnerData()
        {
            InnerValue1 = 0;
            InnerValue2 = "";
        }

        public SomeInnerData(Int32 value1, String value2)
        {
            InnerValue1 = value1;
            InnerValue2 = value2;
        }

        public Int32 InnerValue1 { get; set; }
        public String InnerValue2 { get; set; }
    }

    public class SomeOuterData
    {
        public SomeOuterData()
        {
            OuterValue1 = 0;
            OuterValue2 = "";
            InnerData = new SomeInnerData();
        }

        public SomeOuterData(Int32 value1, String value2)
        {
            OuterValue1 = value1;
            OuterValue2 = value2;
            InnerData = new SomeInnerData();
        }

        public Int32 OuterValue1 { get; set; }
        public String OuterValue2 { get; set; }
        public SomeInnerData InnerData { get; set; }
    }

    public class SomeService
    {
        public void Do()
        {
            SomeOuterData outerData = new SomeOuterData();
            outerData.OuterValue1 = 666;
            outerData.OuterValue2 = "SomeOuterData";
            outerData.InnerData = new SomeInnerData();
            outerData.InnerData.InnerValue1 = 777;
            outerData.InnerData.InnerValue2 = "SomeInnerData";
        }

        public void OtherDo(int value)
        {
            SomeOuterData outerData = new SomeOuterData
            {
                OuterValue1 = 666,
                OuterValue2 = nameof(SomeOuterData),
                InnerData = new SomeInnerData(343, "IDCLIP")
                {
                    InnerValue1 = 777,
                    InnerValue2 = nameof(SomeInnerData)
                }
            };
            if (value > 0)
            {
                outerData = new SomeOuterData
                {
                    OuterValue1 = 999,
                    OuterValue2 = nameof(SomeOuterData),
                    InnerData = new SomeInnerData(767 + 13, "DNMD" + "DNMD")
                    {
                        InnerValue1 = 888,
                        InnerValue2 = nameof(SomeInnerData)
                    }
                };
            }
            else
                outerData = new SomeOuterData(13, "Hello there")
                {
                    OuterValue1 = 121,
                    OuterValue2 = nameof(SomeOuterData),
                    InnerData = new SomeInnerData
                    {
                        InnerValue1 = 212,
                        InnerValue2 = nameof(SomeInnerData)
                    }
                };
        }
    }
}
