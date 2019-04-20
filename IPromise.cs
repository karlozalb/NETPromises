using System;

public interface IPromise
{
    
    IPromise<TResult> Then<TResult>(Func<IPromise<TResult>> onSuccess, Func<Exception, IPromise<TResult>> onError = null);

    IPromise Then(Action onSuccess, Action<Exception> onError = null);
    
    IPromise Then(Func<IPromise> onSuccess, Action<Exception> onError = null);

    IPromise<T> Then<T>(Func<T> onSuccess, Action<Exception> onError = null);
    
    IPromise Catch(Action<Exception> onError);
    
}

public interface IPromise<T>
{

    void Resolve(T param);

    void Reject(Exception error);
    
    IPromise<TResult> Then<TResult>(Func<T, IPromise<TResult>> onSuccess, Func<Exception, IPromise<TResult>> onError = null);
    
    IPromise<TResult> Then<TResult>(Func<IPromise<TResult>> onSuccess, Func<Exception, IPromise<TResult>> onError = null);

    IPromise Then(Action<T> onSuccess, Action<Exception> onError = null);
    
    IPromise Then(Action onSuccess, Action<Exception> onError = null);
    
    IPromise Then(Func<IPromise> onSuccess, Action<Exception> onError = null);

    IPromise Catch(Action<Exception> onError);
    
    IPromise<T> Catch(Func<Exception, T> onError);

}
