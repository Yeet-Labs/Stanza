using System;

namespace Stanza.Definitions
{
    public class Provided
    {
        // public static Validation<string> Donkey { get; } = new Validation<string>
        // {
           
        // };
           
        // public static Validator<string> Potato { get; } = chicken => "";
           
        // public static Validator<string> Tomato { get; } = chicken => new Validation<string>
        // {
        //     tomato => "",
        //     ""
        // };

        public static Validator<string> Hydrated { get; } = value => value switch
        {
            null => "{0} is required.",
            "" => "{0} cannot be empty.",
            { } when String.IsNullOrWhiteSpace(value) => "{0} cannot be blank.",
            _ => true
        };

        public static Builder<string, (int Target, bool Minimum)> Length { get; } = length => new Validation<string>(true)
        {
            Hydrated,
            value => (length.Minimum, value!) switch
            {
                (true, { }) when value.Length < length.Target => $"{{0}} is only {value.Length} characters long, but must be at least {length.Target}.",
                (false, { }) when value.Length > length.Target => $"{{0}} is {value.Length} characters long, must be less than or equal to {length.Target}.",
                _ => true
            }
        };

        public static Generator<string, Range> Range { get; } = range => value => new Validation<string>(true)
        {
            Hydrated,
            Length((range.Start.GetRelativeIndex(value.Length), Minimum: true)),
            Length((range.End.GetRelativeIndex(value.Length), false))
        };
    }
}