﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Manatee.Json.Internal;
using Manatee.Json.Pointer;
using Manatee.Json.Serialization;

namespace Manatee.Json.Schema
{
	/// <summary>
	/// Defines the `unevaluatedProperties` JSON Schema keyword.
	/// </summary>
	[DebuggerDisplay("Name={Name}")]
	public class UnevaluatedPropertiesKeyword : IJsonSchemaKeyword, IEquatable<UnevaluatedPropertiesKeyword>
	{
		/// <summary>
		/// Gets or sets the error message template.
		/// </summary>
		/// <remarks>
		/// Does not supports any tokens.
		/// </remarks>
		public static string ErrorTemplate { get; set; } = "Any properties not covered by `properties`, `patternProperties`, and `additionalProperties` failed validation.";

		/// <summary>
		/// Gets the name of the keyword.
		/// </summary>
		public string Name => "unevaluatedProperties";
		/// <summary>
		/// Gets the versions (drafts) of JSON Schema which support this keyword.
		/// </summary>
		public JsonSchemaVersion SupportedVersions { get; } = JsonSchemaVersion.Draft2019_09;
		/// <summary>
		/// Gets the a value indicating the sequence in which this keyword will be evaluated.
		/// </summary>
		public int ValidationSequence => int.MaxValue;
		/// <summary>
		/// Gets the vocabulary that defines this keyword.
		/// </summary>
		public SchemaVocabulary Vocabulary => SchemaVocabularies.Validation;

		/// <summary>
		/// The schema value for this keyword.
		/// </summary>
		public JsonSchema Value { get; private set; }

		/// <summary>
		/// Used for deserialization.
		/// </summary>
		[DeserializationUseOnly]
		public UnevaluatedPropertiesKeyword() { }
		/// <summary>
		/// Creates an instance of the <see cref="UnevaluatedPropertiesKeyword"/>.
		/// </summary>
		public UnevaluatedPropertiesKeyword(JsonSchema value)
		{
			Value = value;
		}

		/// <summary>
		/// Provides the validation logic for this keyword.
		/// </summary>
		/// <param name="context">The context object.</param>
		/// <returns>Results object containing a final result and any errors that may have been found.</returns>
		public SchemaValidationResults Validate(SchemaValidationContext context)
		{
			if (context.Instance.Type != JsonValueType.Object) return new SchemaValidationResults(Name, context);

			var obj = context.Instance.Object;
			var toEvaluate = obj.Where(kvp => !context.EvaluatedPropertyNames.Contains(kvp.Key)).ToJson();

			var results = new SchemaValidationResults(Name, context);
			if (Value == JsonSchema.False && toEvaluate.Any())
			{
				results.IsValid = false;
				results.Keyword = Name;
				results.ErrorMessage = ErrorTemplate;
				return results;
			}

			var valid = true;
			var reportChildErrors = JsonSchemaOptions.ShouldReportChildErrors(this, context);
			var nestedResults = new List<SchemaValidationResults>();

			foreach (var kvp in toEvaluate)
			{
				var newContext = new SchemaValidationContext(context)
					{
						Instance = kvp.Value,
						BaseRelativeLocation = context.BaseRelativeLocation.CloneAndAppend(Name),
						RelativeLocation = context.RelativeLocation.CloneAndAppend(Name),
						InstanceLocation = context.InstanceLocation.CloneAndAppend(kvp.Key),
					};
				var localResults = Value.Validate(newContext);
				valid &= localResults.IsValid;

				if (JsonSchemaOptions.OutputFormat == SchemaValidationOutputFormat.Flag)
				{
					if (!valid) break;
				}
				else if (reportChildErrors)
					nestedResults.Add(localResults);
			}

			results.NestedResults = nestedResults;

			if (nestedResults.Any(r => !r.IsValid))
			{
				results.IsValid = false;
				results.Keyword = Name;
				results.ErrorMessage = ErrorTemplate;
			}

			return results;
		}
		/// <summary>
		/// Used register any subschemas during validation.  Enables look-forward compatibility with `$ref` keywords.
		/// </summary>
		/// <param name="baseUri">The current base URI</param>
		/// <param name="localRegistry">A local schema registry to handle cases where <paramref name="baseUri"/> is null.</param>
		public void RegisterSubschemas(Uri baseUri, JsonSchemaRegistry localRegistry)
		{
			Value.RegisterSubschemas(baseUri, localRegistry);
		}
		/// <summary>
		/// Resolves any subschemas during resolution of a `$ref` during validation.
		/// </summary>
		/// <param name="pointer">A <see cref="JsonPointer"/> to the target schema.</param>
		/// <param name="baseUri">The current base URI.</param>
		/// <returns>The referenced schema, if it exists; otherwise null.</returns>
		public JsonSchema ResolveSubschema(JsonPointer pointer, Uri baseUri)
		{
			return Value.ResolveSubschema(pointer, baseUri);
		}
		/// <summary>
		/// Builds an object from a <see cref="JsonValue"/>.
		/// </summary>
		/// <param name="json">The <see cref="JsonValue"/> representation of the object.</param>
		/// <param name="serializer">The <see cref="JsonSerializer"/> instance to use for additional
		/// serialization of values.</param>
		public void FromJson(JsonValue json, JsonSerializer serializer)
		{
			Value = serializer.Deserialize<JsonSchema>(json);
		}
		/// <summary>
		/// Converts an object to a <see cref="JsonValue"/>.
		/// </summary>
		/// <param name="serializer">The <see cref="JsonSerializer"/> instance to use for additional
		/// serialization of values.</param>
		/// <returns>The <see cref="JsonValue"/> representation of the object.</returns>
		public JsonValue ToJson(JsonSerializer serializer)
		{
			return Value.ToJson(serializer);
		}
		/// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
		/// <param name="other">An object to compare with this object.</param>
		/// <returns>true if the current object is equal to the <paramref name="other">other</paramref> parameter; otherwise, false.</returns>
		public bool Equals(UnevaluatedPropertiesKeyword other)
		{
			if (other is null) return false;
			if (ReferenceEquals(this, other)) return true;
			return Equals(Value, other.Value);
		}
		/// <summary>Indicates whether the current object is equal to another object of the same type.</summary>
		/// <param name="other">An object to compare with this object.</param>
		/// <returns>true if the current object is equal to the <paramref name="other">other</paramref> parameter; otherwise, false.</returns>
		public bool Equals(IJsonSchemaKeyword other)
		{
			return Equals(other as UnevaluatedPropertiesKeyword);
		}
		/// <summary>Determines whether the specified object is equal to the current object.</summary>
		/// <param name="obj">The object to compare with the current object.</param>
		/// <returns>true if the specified object  is equal to the current object; otherwise, false.</returns>
		public override bool Equals(object obj)
		{
			return Equals(obj as UnevaluatedPropertiesKeyword);
		}
		/// <summary>Serves as the default hash function.</summary>
		/// <returns>A hash code for the current object.</returns>
		public override int GetHashCode()
		{
			return (Value != null ? Value.GetHashCode() : 0);
		}
	}
}