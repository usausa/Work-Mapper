﻿namespace WorkMapper.Expressions
{
    using System;
    using System.Linq.Expressions;

    using WorkMapper.Functions;

    public interface IMappingExpression<TSource, TDestination>
    {
        //--------------------------------------------------------------------------------
        //  Factory
        //--------------------------------------------------------------------------------

        IMappingExpression<TSource, TDestination> FactoryUsing(Func<TDestination> factory);

        IMappingExpression<TSource, TDestination> FactoryUsing(Func<TSource, TDestination> factory);

        IMappingExpression<TSource, TDestination> FactoryUsing(IObjectFactory<TDestination> factory);

        IMappingExpression<TSource, TDestination> FactoryUsing<TObjectFactory>()
            where TObjectFactory : IObjectFactory<TDestination>;

        //--------------------------------------------------------------------------------
        // Pre/Post process
        //--------------------------------------------------------------------------------

        IMappingExpression<TSource, TDestination> BeforeMap(Action<TSource, TDestination> action);

        IMappingExpression<TSource, TDestination> BeforeMap(IMappingAction<TSource, TDestination> action);

        IMappingExpression<TSource, TDestination> BeforeMap<TMappingAction>(IMappingAction<TSource, TDestination> action);

        IMappingExpression<TSource, TDestination> BeforeMap<TMappingAction>()
            where TMappingAction : IMappingAction<TSource, TDestination>;

        IMappingExpression<TSource, TDestination> AfterMap(Action<TSource, TDestination> action);

        IMappingExpression<TSource, TDestination> AfterMap(IMappingAction<TSource, TDestination> action);

        IMappingExpression<TSource, TDestination> AfterMap<TMappingAction>(IMappingAction<TSource, TDestination> action);

        IMappingExpression<TSource, TDestination> AfterMap<TMappingAction>()
            where TMappingAction : IMappingAction<TSource, TDestination>;

        //--------------------------------------------------------------------------------
        // Match
        //--------------------------------------------------------------------------------

        IMappingExpression<TSource, TDestination> MatchMember(Func<string, string> function);

        //--------------------------------------------------------------------------------
        // Member
        //--------------------------------------------------------------------------------

        IMappingExpression<TSource, TDestination> ForMember<TMember>(Expression<Func<TDestination, TMember>> expression, Action<IMemberExpression<TSource, TDestination, TMember>> option);

        //--------------------------------------------------------------------------------
        // Default
        //--------------------------------------------------------------------------------

        // TODO
    }
}
