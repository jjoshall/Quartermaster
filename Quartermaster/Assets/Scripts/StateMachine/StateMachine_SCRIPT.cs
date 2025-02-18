using System;
using System.Collections.Generic;

public class StateMachine {
    StateNode current;
    // Establish tree of nodes via dictionary mapping GetType(State) to corresponding StateNode
    Dictionary<Type, StateNode> nodes = new();
    // Set of transitions any state can use (such as transitioning to death state upon hp < 0)
    HashSet<ITransition> anyTransitions = new();

    public void Update() {
        // see if current state can be transitioned out of
        ITransition transition = GetTransition();
        if (transition != null) {
            ChangeState(transition.To);
        }

        current.State?.Update();
    }
    public void FixedUpdate() {
        current.State?.FixedUpdate();
    }

    // Force change state without transitioning out of previous one
    public void SetState(IState state) {
        current = nodes[state.GetType()];
        current.State?.OnEnter();
    }

    public void ChangeState(IState state) {
        // disable change to same state
        if (state == current.State) return;

        IState previousState = current.State;
        IState nextState = nodes[state.GetType()].State;
        previousState?.OnExit();
        nextState?.OnEnter();
        current = nodes[state.GetType()];
    }

    public void AddTransition(IState from, IState to, IPredicate condition) {
        GetOrAddNode(from).AddTransition(GetOrAddNode(to).State , condition);
    }
    public void AddAnyTransition(IState to, IPredicate condition) {
        anyTransitions.Add(new Transition(GetOrAddNode(to).State, condition));
    }

    // HELPERS
    ITransition GetTransition() {
        // check the any transitions
        foreach (ITransition transition in anyTransitions) {
            if (transition.Condition.Evaluate()) {
                return transition;
            }
        }
        // check state specific transitions
        foreach (ITransition transition in current.Transitions) {
            if (transition.Condition.Evaluate()) {
                return transition;
            }
        }
        // return null if no valid transitions could be found
        return null;
    }

    StateNode GetOrAddNode(IState state) {
        // get null if node does not exist
        StateNode node = nodes.GetValueOrDefault(state.GetType());
        if (node == null) {
            node = new StateNode(state);
            nodes.Add(state.GetType(), node);
        }
        return node;
    }


    class StateNode {
        public IState State { get; }
        // set of transitions this state can use to transit to other states
        public HashSet<ITransition> Transitions { get; }

        public StateNode(IState state) {
            State = state;
            Transitions = new HashSet<ITransition>();
        }

        public void AddTransition(IState to, IPredicate condition) {
            Transitions.Add(new Transition(to , condition));
        }
    }
}