using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Transformalize.Providers.Console;
using Transformalize.Contracts;
using Transformalize.Containers.Autofac;
using Autofac;
using System.Collections.Generic;
using Transformalize.Providers.File.Transforms;
using Transformalize.Transforms.Metadata;
using Transformalize.Configuration;

namespace Test.Unit {
   [TestClass]
   public class UnitTest1 {
      [TestMethod]
      public void TestMethod1() {
         var cfg = @"<cfg name='test'>
   <entities>
      <add name='test'>
         <rows>
            <add file='files\IMG-2884.JPG' />
         </rows>
         <fields>
            <add name='file' />
         </fields>
         <calculated-fields>
            <add name='bytes' type='byte[]' length='max' t='copy(file).fileReadAllBytes()' output='false' />
            <add name='temp' output='false'>
               <transforms>
                  <add method='frommetadata'>
                     <parameters>
                        <add field='bytes' />
                     </parameters>
                     <fields>
                        <add name='Make' />
                        <add name='Model' />
                        <add name='GPS Latitude' class='GPS' type='double' />
                        <add name='GPS Longitude' class='GPS' type='double' />
                     </fields>
                  </add>
               </transforms>
            </add>
         </calculated-fields>
      </add>
   </entities>
</cfg>";

         var logger = new ConsoleLogger(LogLevel.Debug);

         var transforms = new List<TransformHolder> {
            new TransformHolder((c) => new FileReadAllBytesTransform(c), new FileReadAllBytesTransform().GetSignatures()),
            new TransformHolder((c) => new FromMetadataTransform(c), new FromMetadataTransform().GetSignatures())
         }.ToArray();

         using (var outer = new ConfigurationContainer(transforms).CreateScope(cfg, logger)) {
            var process = outer.Resolve<Process>();
            using(var inner = new Container(transforms).CreateScope(process, logger)) {
               inner.Resolve<IProcessController>().Execute();
               var rows = process.Entities[0].Rows;
               Assert.AreEqual(1, rows.Count);
               Assert.AreEqual("Apple", rows[0]["Make"]);
               Assert.AreEqual("iPhone XS", rows[0]["Model"]);
               Assert.AreEqual((double)42.080733, Math.Round((double) rows[0]["GPS Latitude"],6));
               Assert.AreEqual((double)-86.481728, Math.Round((double) rows[0]["GPS Longitude"],6));
            }
         }
         
      }
   }
}
