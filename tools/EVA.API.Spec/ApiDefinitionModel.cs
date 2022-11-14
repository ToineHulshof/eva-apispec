﻿using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace EVA.API.Spec;

public class ApiDefinitionModel
{
  [JsonPropertyName("apiversion")] public int ApiVersion { get; set; }
  [JsonPropertyName("services")] public ImmutableArray<ServiceModel> Services { get; set; }
  [JsonPropertyName("types")] public ImmutableSortedDictionary<string, TypeSpecification> Types { get; set; }
  [JsonPropertyName("event_targets")] public ImmutableArray<EventTarget> EventTargets { get; set; }
  [JsonPropertyName("errors")] public ImmutableArray<ErrorSpecification> Errors { get; set; }
}