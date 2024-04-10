using System.ComponentModel;

namespace Hyperbee.Pipeline.Data;

internal static class Converter
{
    internal static bool TryConvertTo<TOutput>( object argument, out TOutput converted )
    {
        try
        {
            // quick conversions

            switch ( argument )
            {
                case null:
                    return SetResult( out converted, default );

                case TOutput result:
                    return SetResult( out converted, result );

                case IConvertible:
                    return SetResult( out converted, (TOutput) Convert.ChangeType( argument, typeof( TOutput ) ) );
            }

            // try and get a converter for this type

            var converter = TypeDescriptor.GetConverter( typeof( TOutput ) );

            if ( converter.CanConvertFrom( argument.GetType() ) )
            {
                try
                {
                    return SetResult( out converted, (TOutput) converter.ConvertFrom( argument ) );
                }
                catch
                {
                    // conversion can fail 
                }
            }

            // try and get a converter for this instance

            converter = TypeDescriptor.GetConverter( argument );

            if ( converter.CanConvertTo( typeof( TOutput ) ) )
            {
                try
                {
                    return SetResult( out converted, (TOutput) converter.ConvertTo( argument, typeof( TOutput ) ) );
                }
                catch
                {
                    // conversion can fail 
                }
            }
        }
        catch
        {
            // conversion can fail 
        }

        return SetResult( out converted, default, false );

        // result helper 

        static bool SetResult( out TOutput result, TOutput value, bool didConvert = true )
        {
            result = value;
            return didConvert;
        }
    }
}
