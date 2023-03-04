using System.Collections.Generic;

public class StateMachine<T1, T2> where T2 : StateMachine<T1, T2>.IState
{

    public T2 Current { get; private set; }


    private Dictionary<T1, T2> states;

    public StateMachine()
    {
        states = new Dictionary<T1, T2>();
    }

    public void SetState(T1 key) => Current = states[key];
    public void SetState(T2 value) => Current = value;
    public void SwitchState(T1 nextKey)
    {
        T2 next = states[nextKey];
        Current.Exit(next);
        next.Enter(Current);
        Current = next;
    }

    public void AddState(T1 key, T2 value) => states.Add(key, value);
    public void RemoveState(T1 key) => states.Remove(key);

    public interface IState
    {
        void Enter(T2 previous);
        void Exit(T2 next);
    }

}