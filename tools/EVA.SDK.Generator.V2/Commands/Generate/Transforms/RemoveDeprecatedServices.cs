﻿using System.Collections.Immutable;
using EVA.API.Spec;
using Microsoft.Extensions.Logging;

namespace EVA.SDK.Generator.V2.Commands.Generate.Transforms;

public class RemoveDeprecatedServices : INamedTransform
{
  public string Name => "deprecated-services";
  public string Description => "Will remove all deprecated services";

  public ITransform.TransformResult Transform(ApiDefinitionModel input, GenerateOptions options, ILogger logger)
  {
    var countBefore = input.Services.Length;
    input.Services = input.Services.Where(s => s.Deprecated == null).ToImmutableArray();

    return countBefore == input.Services.Length ? ITransform.TransformResult.NoChanges : ITransform.TransformResult.Changes;
  }
}