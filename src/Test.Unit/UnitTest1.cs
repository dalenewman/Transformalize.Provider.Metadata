using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Transformalize.Providers.Console;
using Transformalize.Contracts;
using Transformalize.Containers.Autofac;
using Autofac;
using System.Collections.Generic;
using Transformalize.Providers.File.Transforms;
using Transformalize.Transform.Metadata;
using Transformalize.Configuration;
using System.Linq;

namespace Test.Unit {
   [TestClass]
   public class UnitTest1 {
      [TestMethod]
      public void TestMethod1() {
         var cfg = @"<cfg name='test'>
   <entities>
      <add name='test'>
         <rows>
            <add file='files\gavin.jpg' />
            <add file='C:\Users\dnewman\Downloads\C400433142_Skip_20190506114902.jpg' />
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
                        <add name='Megapixels' />
                        <add name='GPS Latitude' class='GPS' type='double' />
                        <add name='GPS Longitude' class='GPS' type='double' />
                        <add name='Make' />
                        <add name='Model' />
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
               var output = inner.Resolve<IProcessController>().Read().ToArray();
               Assert.AreEqual(2, output.Length);
            }
         }
         
      }
   }
}
