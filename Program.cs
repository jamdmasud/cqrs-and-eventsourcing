using System;
using System.Collections.Generic;
using System.Linq;

namespace cqrs_and_eventsourcing
{
    public class Person 
    {
        private int age;
        EventBroker broker;
        public Person(EventBroker broker)
        {
            this.broker = broker;
            this.broker.Commands += BrokerOnCommands;
            this.broker.Queries +=  BrokerQueries;
        }

        private void BrokerQueries(object sender, Query query)
        {
            var ac = query as AgeQuery;
            if(ac != null && ac.Target == this) 
            {
                ac.Result = age;
            }
        }

        private void BrokerOnCommands(object sender, Command command)
        {
            var cac = command as ChangeAgeCommand;
            if(cac != null && cac.Target == this)
            {
                if(cac.Register) broker.AllEvents.Add(new AgeChangedEvent(this, age, cac.Age));
                age = cac.Age;
            }
        }
    }

    public class EventBroker 
    {
        // 1. All event that happened.
        public IList<Event> AllEvents = new List<Event>();
        // 2. Command 
        public event EventHandler<Command> Commands;
        // 3. Query
        public event EventHandler<Query> Queries;

        public void Command(Command c)
        {
            Commands?.Invoke(this, c);
        }

        public T Query<T>(Query q)
        {
            Queries?.Invoke(this, q);
            return (T) q.Result;
        }

        public void UndoLast()
        {
            var e = AllEvents.LastOrDefault();
            var ac = e as AgeChangedEvent;
            if(ac != null) 
            {
                Command(new ChangeAgeCommand(ac.Target, ac.OldAge) { Register = false });
                AllEvents.Remove(e);
            }
        }
    }

    public class Query
    {
        public object Result;
    }

    public class AgeQuery : Query
    {
        public Person Target;
    }

    public class Command : EventArgs
    {
        public bool Register = true;
    }

    public class ChangeAgeCommand : Command
    {
        public Person   Target;
        public int Age;

        public ChangeAgeCommand(Person target, int age) 
        {
            Target = target;
            Age = age;
        }
    }

    public class Event
    {
        //Back track
    }

    public class AgeChangedEvent : Event
    {
        public Person Target;
        public int OldAge, NewAge;
        public AgeChangedEvent(Person target, int oldAge, int newAge)
        {
            this.Target = target;
            this.OldAge = oldAge;
            this.NewAge = newAge;
        }
        
        public override string ToString()
        {
            return $"Changed value from {OldAge} to {NewAge}";
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            var eb = new EventBroker();
            var p = new Person(eb);
            eb.Command(new ChangeAgeCommand(p, 123));
            foreach(var e in eb.AllEvents){
                Console.WriteLine(e);
            }
            int age = eb.Query<int>(new AgeQuery{Target = p});
            Console.WriteLine(age);

            eb.UndoLast();

            foreach(var e in eb.AllEvents){
                Console.WriteLine(e);
            }
            age = eb.Query<int>(new AgeQuery{Target = p});
            Console.WriteLine(age);

        }
    }
}
