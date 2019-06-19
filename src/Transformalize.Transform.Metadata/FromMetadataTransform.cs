using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Transformalize.Configuration;
using Transformalize.Contracts;

namespace Transformalize.Transforms.Metadata {
   public class FromMetadataTransform : BaseTransform {

      private readonly Field _input;
      private readonly Field[] _output;
      private readonly Dictionary<string, Dictionary<string, Field>> _lookup = new Dictionary<string, Dictionary<string, Field>>(StringComparer.OrdinalIgnoreCase);
      private readonly bool _gpsCheck;

      public FromMetadataTransform(IContext context = null) : base(context, null) {
         if (IsMissingContext()) {
            return;
         }

         ProducesFields = true;

         if (!Context.Operation.Parameters.Any()) {
            Error($"The {Context.Operation.Method} transform requires a collection of output fields.");
            Run = false;
            return;
         }

         if (IsNotReceiving("byte[]")) {
            Error("The fromMetadata transform only accepts a byte array as input.");
            return;
         }

         _input = SingleInputForMultipleOutput();
         _output = MultipleOutput();
         _gpsCheck = _output.Any(f => f.Name.StartsWith("GPS", StringComparison.OrdinalIgnoreCase) && f.Type == "double");

         foreach (var field in _output) {
            if (_lookup.ContainsKey(field.Class)) {
               if (!_lookup[field.Class].ContainsKey(field.Name)) {
                  _lookup[field.Class].Add(field.Name, field);
               }
            } else {
               _lookup.Add(field.Class, new Dictionary<string, Field>(StringComparer.OrdinalIgnoreCase) {
                  { field.Name, field }
               });
            }
         }
         if (!_lookup.ContainsKey(string.Empty)) {
            _lookup.Add(string.Empty, new Dictionary<string, Field>(StringComparer.OrdinalIgnoreCase));
         }
      }

      public override IRow Operate(IRow row) {

         var bytes = (byte[])row[_input];
         IReadOnlyList<MetadataExtractor.Directory> directories = null;

         using (var stream = new MemoryStream(bytes)) {
            try {
               directories = ImageMetadataReader.ReadMetadata(stream);
            } catch (ImageProcessingException ex) {
               Context.Error(ex.Message);
               Context.Debug(() => ex.StackTrace);
            }
         }

         if (directories != null) {

            foreach (var directory in directories) {

               var lookup = _lookup.ContainsKey(directory.Name) ? _lookup[directory.Name] : _lookup[string.Empty];

               foreach (var tag in directory.Tags) {
                  
                  // Context.Debug(() => $"Tag:{tag.Name},Value:{tag.Description}");

                  if (lookup.ContainsKey(tag.Name)) {

                     var field = lookup[tag.Name];

                     /* GPS coordinates are stored in two fields, a direction (e.g. N,W), 
                        and a string in degrees, minutes, and seconds format */
                     if (directory.Name=="GPS" && _gpsCheck && field.Type == "double" && directory is GpsDirectory gps) {
                        var geo = gps.GetGeoLocation();
                        switch (tag.Name) {
                           case "GPS Latitude":
                              row[field] = geo.Latitude;
                              break;
                           case "GPS Longitude":
                              row[field] = geo.Longitude;
                              break;
                           default:
                              row[field] = tag.Description;
                              break;
                        }
                     } else {
                        row[field] = tag.Description;
                     }

                  }
               }
            }
         }

         return row;

      }

      public override IEnumerable<OperationSignature> GetSignatures() {
         yield return new OperationSignature("frommetadata");
      }
   }
}
