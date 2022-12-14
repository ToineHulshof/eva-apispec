﻿using System.Collections.Immutable;
using System.Text.Json;
using EVA.API.Spec;
using EVA.SDK.Generator.V2.Commands.Generate.Helpers;

namespace EVA.SDK.Generator.V2.Commands.Generate.Generator.Transforms;

public class RemoveSharedRequestResponseTypes : ITransform
{
  public ITransform.TransformResult Transform(ApiDefinitionModel input, GenerateOptions options)
  {
    ApiSpecHelpers.RebuildTypeContext(input);

    var changes = ITransform.TransformResult.NoChanges;
    var renameCache = new Dictionary<string, string>();

    // Split all types into request and response types
    var newTypes = new Dictionary<string, TypeSpecification>(input.Types);
    foreach (var (typeID, typeSpec) in input.Types)
    {
      if (!typeSpec.EnumIsFlag.HasValue && typeSpec.Usage is { Request: true, Response: true })
      {
        // Create a copy
        var responseTypeSpec = JsonSerializer.Deserialize(JsonSerializer.Serialize(typeSpec, IndentedSerializationHelper.Default.TypeSpecification), IndentedSerializationHelper.Default.TypeSpecification)!;
        typeSpec.Usage.Response = false;
        responseTypeSpec.Usage.Request = false;

        // Find new ID and name
        var responseTypeID = typeID;
        var addedResponses = 0;
        while(newTypes.ContainsKey(responseTypeID))
        {
          responseTypeID = AddResponse(responseTypeID);
          addedResponses++;
        }
        renameCache.Add(typeID, responseTypeID);

        for (var i = 0; i < addedResponses; i++) responseTypeSpec.TypeName = AddResponse(responseTypeSpec.TypeName);
        newTypes.Add(responseTypeID, responseTypeSpec);
      }
    }

    if (!renameCache.Any()) return ITransform.TransformResult.NoChanges;
    input.Types = newTypes.ToImmutableSortedDictionary();

    // Update the referenced
    foreach (var (id,typeSpec) in input.Types)
    {
      if (!typeSpec.Usage.Response) continue;

      if(typeSpec.ParentType != null && renameCache.TryGetValue(typeSpec.ParentType, out var newParentType))
      {
        typeSpec.ParentType = newParentType;
        changes = ITransform.TransformResult.Changes;
      }

      foreach (var typeReference in typeSpec.EnumerateAllTypeReferences())
      {
        if(renameCache.TryGetValue(typeReference.Name, out var rename))
        {
          typeReference.Name = rename;
          changes = ITransform.TransformResult.Changes;
        }
      }
    }

    return changes;
  }

  private static string AddResponse(string s)
  {
    var idx = s.IndexOf('`');
    if(idx == -1) return s + "Response";

    var idx2 = s.LastIndexOf('+');
    if(idx2 > idx) return s + "Response";

    return s[..idx] + "Response" + s[idx..];
  }
}