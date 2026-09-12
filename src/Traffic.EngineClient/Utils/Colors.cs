using System;
using Godot;


namespace RoadTrafficSim.Utils;


public delegate TState Reducer<TState, TArg>(params TArg[] args);

public static class ReducerFactory
{
    public static Reducer<TState, TArg> Create<TState, TArg>(
        TState initial,
        Func<TState, TArg, TState> mutation
    )
    {
        var current = initial;
        return (args) =>
        {
            if (args.Length > 0)
            {
                current = mutation(current, args[0]);
            }

            return current;
        };
    }
    
    public static Reducer<T, T> Create<T>(T initial, Func<T, T, T> mutation)
        => Create<T, T>(initial, mutation);
}


public static class Colors
{
    private static readonly Func<Color, float, Color> Mutate = (color, alpha) => { color.A = alpha; return color; };
    
    public static readonly Reducer<Color, float> Cyan = ReducerFactory.Create(
        new Color(0.2f, 0.8f, 1.0f), Mutate
    );

    public static readonly Reducer<Color, float> White = ReducerFactory.Create(
        new Color(0.9f, 0.9f, 0.9f), Mutate
    );
    
    public static readonly Reducer<Color, float> Yellow = ReducerFactory.Create(
        new Color(1.0f, 1.0f, 0.2f), Mutate
    );

    public static readonly Reducer<Color, float> RadiantGreen = ReducerFactory.Create(
        new Color(0.2f, 1.0f, 0.3f), Mutate
    );
    
    public static readonly Reducer<Color, float> Red = ReducerFactory.Create(
        new Color(1.0f, 0.2f, 0.2f), Mutate
    );
}