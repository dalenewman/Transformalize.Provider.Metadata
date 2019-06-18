using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Transformalize.Configuration;
using Transformalize.Contracts;
using Transformalize.Transforms;

namespace Transformalize.Transform.Metadata {
   public class FromMetadataTransform : BaseTransform {

      private readonly Field _input;
      private readonly Field[] _output;
      private readonly Dictionary<string, Dictionary<string, Field>> _lookup = new Dictionary<string, Dictionary<string, Field>>(StringComparer.OrdinalIgnoreCase);

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
               Context.Debug(() => "Directory: " + directory.Name);
               var lookup = _lookup.ContainsKey(directory.Name) ? _lookup[directory.Name] : _lookup[string.Empty];
               foreach (var tag in directory.Tags) {
                  Context.Debug(() => " Tag: " + tag.Name + ", Value:" + tag.Description);
                  if (lookup.ContainsKey(tag.Name)) {
                     var field = lookup[tag.Name];
                     if (directory.Name == "GPS" && field.Type == "double" && directory is GpsDirectory gps) {
                        var geo = gps.GetGeoLocation();
                        if (tag.Name == "GPS Latitude") {
                           row[field] = geo.Latitude;
                        } else if (tag.Name == "GPS Longitude") {
                           row[field] = geo.Longitude;
                        } else {
                           row[field] = tag.Description;
                        }
                     } else {
                        row[field] = tag.Description;
                     }
                     Context.Debug(() => $"  Storing {tag.Description} into {field.Alias}.");
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
