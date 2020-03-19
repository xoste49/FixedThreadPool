using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FixedThreadPool
{
   /* Требуется реализация класса на языке C#, аналогичного FixedThreadPool в Java, со следующими требованиями:
   * + В конструктор этого класса должно передаваться количество потоков, которые будут выполнять задачи.
   * + Интерфейс класса должен предоставлять методы: bool Execute(Task task, Priority priority) и void Stop()
   * + Интерфейс Task должен содержать один метод: void Execute(), который вызывается в произвольном потоке.
   * + Тип Priority — это перечисление из трёх приоритетов: HIGH, NORMAL, LOW. При этом во время выбора следующего задания из очереди действуют такие правила: 
   * на три задачи с приоритетом HIGH выполняется одна задача с приоритетом NORMAL, 
   * + задачи с приоритетом LOW не выполняются, пока в очереди есть хоть одна задача с другим приоритетом.
   * + До вызова метода Stop() задачи ставятся в очередь на выполнение и метод bool Execute(Task task, Priority priority) сразу же возвращает true, не дожидаясь завершения выполнения задачи; 
   * + а после вызова Stop() новые задачи не добавляются в очередь на выполнение, 
   * + и метод bool Execute(Task task, Priority priority) сразу же возвращает false.
   * + Метод Stop() ожидает завершения всех текущих задач (не очищая очередь).
   */

   // Приорететы для потоков
   public enum Priority
   {
      // Низкий приоритет.
      Low,
      // Средний приоритет.
      Normal,
      // Высокий приоритет.
      High
   }
   interface IFixedThreadPool
   {
      Task<bool> Execute(ITask task, Priority priority);
      void Stop();
   }
   /// <summary>
   /// Задача для вызова в произвольном потоке
   /// </summary>
   interface ITask
   {
      void Execute();
   }

   class FixedThreadPool : IFixedThreadPool
   {
      // Флаг остановки
      bool isStop = false;
      int countThreads;
      // Очереди по приорететам (потокобезопасный)
      ConcurrentQueue<ITask> highPriorityTasks = new ConcurrentQueue<ITask>();
      ConcurrentQueue<ITask> normalPriorityTasks = new ConcurrentQueue<ITask>();
      ConcurrentQueue<ITask> lowPriorityTasks = new ConcurrentQueue<ITask>();
      // Задачи для выполнения
      ConcurrentDictionary<ITask, Task> executingTasks = new ConcurrentDictionary<ITask, Task>();

      // Выполнить с указанным приоритетом
      public Task<bool> Execute(ITask task, Priority priority)
      {
         if (task == null) throw new ArgumentNullException("task", "Задача отсутствует");
         // Если вызван Stop() то возращаем False
         if (isStop) return Task.FromResult(false);
         // Раскладываем по приорететам
         switch (priority)
         {
            case Priority.Low:
               lowPriorityTasks.Enqueue(task);
               break;
            case Priority.Normal:
               normalPriorityTasks.Enqueue(task);
               break;
            case Priority.High:
               highPriorityTasks.Enqueue(task);
               break;
            default:
               break;
         }
         return Task.FromResult(true);
      }

      /// <summary>
      /// 
      /// </summary>
      /// <param name="countThreads">Колличество потоков</param>
      public FixedThreadPool(int countThreads)
      {
         if (countThreads <= 0) throw new ArgumentOutOfRangeException("countThreads", "Колличествао потоков должно быть больше 0");
         this.countThreads = countThreads;
         Task.Run(() => ScheduleTask());
      }

      // Планировщик для потоков
      private async Task ScheduleTask()
      {
         // Проверяем на наличие задач
         bool PriorityTasksAny() => highPriorityTasks.Any() || normalPriorityTasks.Any() || lowPriorityTasks.Any();
         int executedHighTaskCount = 0; // Колличество выполненых задач с высоким приоритетом
         int executedNormalTaskCount = 0; // Колличество выполненых задач с нормальным приоритетом

         /* на три задачи с приоритетом HIGH выполняется одна задача с приоритетом NORMAL, */

         while (true)
         {
            // Если не(колличество выполняемых потоков больше/равно самих потоков) и очередь потоков не пустая то выполняем
            if (!(executingTasks.Count > countThreads) && PriorityTasksAny())
            {
               Console.WriteLine(executingTasks.Count + ">=" + countThreads);
               // Если есть задача с высоким приоритетом
               if (highPriorityTasks.Any())
               {
                  // Если задачи с нормальным приоритетом отсутствуют или не(кол-во задач с высоким приорететом > 3) выполням
                  if (!normalPriorityTasks.Any() || !(executedHighTaskCount > 3))
                  {
                     // Если Выполнить задачу будет true значит значит увеличиваем кол-во выполненых задач на 1
                     if (ExecuteTaskFromQueue(highPriorityTasks))
                     {
                        executedHighTaskCount++;
                        continue;
                     }
                  }
               }
               // Если есть задача с нормальным приоритетом
               if (normalPriorityTasks.Any())
               {
                  // Колличество выполненых задач с высоким приоритетом не должно быть больше 3 или не должно быть задач с высоким приоритетом
                  if (executedHighTaskCount >= 3 || !highPriorityTasks.Any())
                     // Если задача выполнилась
                     if (ExecuteTaskFromQueue(normalPriorityTasks))
                     {
                        if (executedHighTaskCount >= 3)
                        {
                           executedHighTaskCount = 0;
                        }

                        continue;
                     }
               }
               // Если есть задача с низким приоритетом и нет в очереди других задач
               if (lowPriorityTasks.Any() && (highPriorityTasks.Count + normalPriorityTasks.Count == 0))
               {
                  // Если задача выполнилась
                  if (ExecuteTaskFromQueue(lowPriorityTasks)) continue;
               }
            } else await Task.Delay(100);
            // Проверка условий для выхода из цикла
            // Если очередь потоков пустая и коллекция поток пустая и флаг isStop = true то выходим из цикла
            if (!PriorityTasksAny() && !executingTasks.Any() && isStop) break;
         }
      }

      // Выполнить задачу из очереди
      private bool ExecuteTaskFromQueue(ConcurrentQueue<ITask> queue)
      {
         // Берем задачу из очереди
         if (queue.TryDequeue(out ITask task))
         {
            // ВЫполням задачу без ожидания
            ExecuteAsync(task).ConfigureAwait(false);
            return true;
         }

         return false;
      }
      // Выполняем задачу
      private async Task ExecuteAsync(ITask task)
      {
         if (executingTasks.TryAdd(task, new Task(task.Execute)))
         {
            Console.WriteLine("Задача добавлена");
            // Ждём пока не выполниться задача
            await Task.Run(() => task.Execute());
            // Удаляем задачу из списка выполняемых задач
            Console.WriteLine("Задача удалена");
            executingTasks.TryRemove(task, out _);
         }
      }

      /// <summary>
      /// Stop() новые задачи не добавляются в очередь на выполнение
      /// </summary>
      public void Stop()
      {
         if (isStop) return;

         isStop = true;
         // Ожидание завершения всех задач
         while (executingTasks.Any())
         {
            Task.Delay(100);
         }
      }

   }

}
