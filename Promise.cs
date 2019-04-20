using System;
using System.Collections.Generic;
using UnityEngine;

public class Promise<T> : IPromise<T>
{

    public enum PromiseStatus
    {
        PENDING,
        RESOLVED,
        REJECTED
    }

    private T m_savedParam;
    private Exception m_savedErrorParam;
    
    private PromiseStatus CurrentStatus;
        
    public delegate void ResolveEvent(T param);
    public delegate void RejectEvent(Exception param);

    private Action<ResolveEvent, RejectEvent> m_resolveAndReject;

    private Action m_onResolved, m_onRejected;

    private List<Action<T>> m_resolvers;
    private List<Action<Exception>> m_rejectors;
   
    public Promise(Action<ResolveEvent,RejectEvent> resolveAndReject)
    {
        CurrentStatus = PromiseStatus.PENDING;

        try
        {
            resolveAndReject(Resolve, Reject);
        }
        catch (Exception exception)
        {
            Reject(exception);
        }
    }

    public Promise()
    {
        CurrentStatus = PromiseStatus.PENDING;
    }

    void SetResolveAndReleject(Action<ResolveEvent,RejectEvent> resolveAndReject)
    {
        m_resolveAndReject = resolveAndReject;
    }
    
    public void Resolve(T param)
    {
        CurrentStatus = PromiseStatus.RESOLVED;
        m_savedParam = param;

        InvokePendingResolver();
    }
        
    public void Reject(Exception param)
    {
        CurrentStatus = PromiseStatus.REJECTED;
        m_savedErrorParam = param;

        InvokePendingRejector();
    }

    public void InvokePendingResolver()
    {
        if (m_resolvers != null && m_resolvers.Count > 0)
        {
            var resolver = m_resolvers[0];
            m_resolvers.RemoveAt(0);
            
            var rejector = m_rejectors[0];
            m_rejectors.RemoveAt(0);

            try
            {
                resolver(m_savedParam);
            }
            catch (Exception e)
            {
                rejector(e);
            }
        }
    }

    public void InvokePendingRejector()
    {
        if (m_rejectors != null && m_rejectors.Count > 0)
        {
            var rejector = m_rejectors[0];
            m_rejectors.RemoveAt(0);
            m_resolvers.RemoveAt(0);
            
            rejector(m_savedErrorParam);
        }
    }
    
    public void InvokeResolverRejected(Action<T> resolver, Action<Exception> rejector)
    {
        if (CurrentStatus == PromiseStatus.PENDING)
        {
            if (m_resolvers == null) m_resolvers = new List<Action<T>>();
            if (m_rejectors == null) m_rejectors = new List<Action<Exception>>();

            m_resolvers.Add(resolver);
            m_rejectors.Add(rejector);
        }
        else if (CurrentStatus == PromiseStatus.RESOLVED)
        {
            resolver(m_savedParam);
        }
        else if (CurrentStatus == PromiseStatus.REJECTED)
        {
            rejector(m_savedErrorParam);
        }
    }

    public IPromise<TResult> Then<TResult>(Func<T,IPromise<TResult>> onSuccess, Func<Exception,IPromise<TResult>> onError = null)
    {
        Promise<TResult> newPromise = new Promise<TResult>();

        void Resolved(T p) => onSuccess(p).Then(value => newPromise.Resolve(value), error => newPromise.Reject(error));
        void Rejected(Exception error)
        {
            if (onError == null)
            {
                newPromise.Reject(error);
            }
            else
            {
                try
                {
                    onError(error).Then(value => newPromise.Resolve(value), exception => newPromise.Reject(exception));
                }
                catch (Exception ex)
                {
                    newPromise.Reject(ex);
                }
            }
        }

        InvokeResolverRejected(Resolved, Rejected);
        
        return newPromise;
    }

    public IPromise<TResult> Then<TResult>(Func<IPromise<TResult>> onSuccess, Func<Exception, IPromise<TResult>> onError = null)
    {
        Promise<TResult> newPromise = new Promise<TResult>();

        void Resolved(T p) => onSuccess().Then(value => newPromise.Resolve(value), error => newPromise.Reject(error));
        void Rejected(Exception error)
        {
            if (onError == null)
            {
                newPromise.Reject(error);
            }
            
            onError?.Invoke(error).Then(value => newPromise.Resolve(value), exception => newPromise.Reject(exception));
        }

        InvokeResolverRejected(Resolved, Rejected);
        
        return newPromise;
    }

    public IPromise Then(Action<T> onSuccess, Action<Exception> onError = null)
    {
        Promise newPromise = new Promise();

        void Resolved(T p) => onSuccess(p);
        void Rejected(Exception error) => onError?.Invoke(error);

        InvokeResolverRejected(Resolved, Rejected);
        
        return newPromise;
    }

    public IPromise Then(Action onSuccess, Action<Exception> onError = null)
    {
        Promise newPromise = new Promise();

        void Resolved(T p) => onSuccess();
        void Rejected(Exception error) => onError?.Invoke(error);

        InvokeResolverRejected(Resolved, Rejected);
        
        return newPromise;
    }

    public IPromise Then(Func<IPromise> onSuccess, Action<Exception> onError = null)
    {
        Promise newPromise = new Promise();

        void Resolved(T p) => onSuccess().Then(() => newPromise.Resolve(), error => newPromise.Reject(error));
        void Rejected(Exception error) => onError?.Invoke(error);

        InvokeResolverRejected(Resolved, Rejected);
        
        return newPromise;
    }

    public IPromise Catch(Action<Exception> onError)
    {
        Promise newPromise = new Promise();

        void Resolved(T p) => newPromise.Resolve();

        void OnRejected(Exception ex)
        {
            try
            {
                onError(ex);
                newPromise.Resolve();
            }
            catch (Exception e)
            {
                newPromise.Reject(e);
            }
        }

        InvokeResolverRejected(Resolved, OnRejected);
        
        return newPromise;
    }

    public IPromise<T> Catch(Func<Exception, T> onError)
    {
        Promise<T> newPromise = new Promise<T>();
        
        void Resolved(T p) => newPromise.Resolve(p);

        void OnRejected(Exception ex)
        {
            try
            {
                newPromise.Resolve(onError(ex));
            }
            catch (Exception e)
            {
                newPromise.Reject(e);
            }
        }

        InvokeResolverRejected(Resolved, OnRejected);
        
        return newPromise;
    }

    void Test()
    {
        
        Promise<int> p1 = new Promise<int>((resolve, reject) => { resolve(1); });
        //Promise<int> p2 = new Promise<int>((resolve, reject) => { resolve(result * 2); });


        p1.Then((result) =>
            {
                Promise<string> p = new Promise<string>((res, rej) => { res("7.0f"); });

                return p;
            })
            .Then((input) =>
            {
                Promise<float> p = new Promise<float>((res, rej) => { res(float.Parse(input)); });

                return p;
            }).Then((input) => { Debug.Log(input * 2f);});



        //p1.Then(p2);
    }
    
}

public class Promise : IPromise
{
    
    private List<Action> m_resolvers;
    private List<Action<Exception>> m_rejectors;
    
    public enum PromiseStatus
    {
        PENDING,
        RESOLVED,
        REJECTED
    }

    private Exception m_savedErrorParam;
    private PromiseStatus CurrentStatus;
        
    public delegate void ResolveEvent();
    public delegate void RejectEvent(Exception param);
    
    public void Resolve()
    {
        CurrentStatus = PromiseStatus.RESOLVED;

        InvokePendingResolver();
    }
        
    public void Reject(Exception param)
    {
        CurrentStatus = PromiseStatus.REJECTED;
        m_savedErrorParam = param;

        InvokePendingRejector();
    }

    public void InvokePendingResolver()
    {
        if (m_resolvers != null && m_resolvers.Count > 0)
        {
            var resolver = m_resolvers[0];
            m_resolvers.RemoveAt(0);
            
            var rejector = m_rejectors[0];
            m_rejectors.RemoveAt(0);

            try
            {
                resolver();
            }
            catch (Exception e)
            {
                rejector(e);
            }
        }
    }
    
    public void InvokePendingRejector()
    {
        if (m_rejectors != null && m_rejectors.Count > 0)
        {
            var rejector = m_rejectors[0];
            m_rejectors.RemoveAt(0);
            m_resolvers.RemoveAt(0);
            
            rejector(m_savedErrorParam);
        }
    }
    
    public void InvokeResolverRejected(Action resolver, Action<Exception> rejector)
    {
        if (CurrentStatus == PromiseStatus.PENDING)
        {
            if (m_resolvers == null) m_resolvers = new List<Action>();
            if (m_rejectors == null) m_rejectors = new List<Action<Exception>>();

            m_resolvers.Add(resolver);
            m_rejectors.Add(rejector);
        }
        else if (CurrentStatus == PromiseStatus.RESOLVED)
        {
            resolver();
        }
        else if (CurrentStatus == PromiseStatus.REJECTED)
        {
            rejector(m_savedErrorParam);
        }
    }
    
    public IPromise<TResult> Then<TResult>(Func<IPromise<TResult>> onSuccess, Func<Exception, IPromise<TResult>> onError = null)
    {
        Promise<TResult> newPromise = new Promise<TResult>();

        void Resolved() => onSuccess().Then(value => newPromise.Resolve(value), error => newPromise.Reject(error));
        void Rejected(Exception error) => onError?.Invoke(error).Then(value => newPromise.Resolve(value), exception => newPromise.Reject(exception));

        InvokeResolverRejected(Resolved, Rejected);
        
        return newPromise;
    }

    public IPromise Then(Action onSuccess, Action<Exception> onError = null)
    {
        Promise newPromise = new Promise();

        void Resolved() => onSuccess();
        void Rejected(Exception error) => onError?.Invoke(error);

        InvokeResolverRejected(Resolved, Rejected);
        
        return newPromise;
    }

    public IPromise Then(Func<IPromise> onSuccess, Action<Exception> onError = null)
    {
        Promise newPromise = new Promise();

        void Resolved() => onSuccess().Then(() => newPromise.Resolve(), error => newPromise.Reject(error));
        void Rejected(Exception error) => onError?.Invoke(error);

        InvokeResolverRejected(Resolved, Rejected);
        
        return newPromise;
    }
    
    public IPromise<T> Then<T>(Func<T> onSuccess, Action<Exception> onError = null)
    {
        IPromise<T> newPromise = new Promise<T>();

        void Resolved() => newPromise.Resolve(onSuccess());
        void Rejected(Exception error) => onError?.Invoke(error);

        InvokeResolverRejected(Resolved, Rejected);
        
        return newPromise;
    }

    public IPromise Catch(Action<Exception> onError)
    {
        Promise newPromise = new Promise();

        void Resolved() => newPromise.Resolve();

        void OnRejected(Exception ex)
        {
            try
            {
                onError(ex);
                newPromise.Resolve();
            }
            catch (Exception e)
            {
                newPromise.Reject(e);
            }
        }

        InvokeResolverRejected(Resolved, OnRejected);
        
        return newPromise;
    }
}

