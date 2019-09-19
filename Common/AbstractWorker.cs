using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OrdinaryDumpDeduplicator.Common
{
    public abstract class AbstractWorker
    {
        //private readonly ILog _log;
        private readonly int _delayInMilliseconds;
        //private RequestModerator _requestModerator;
        private bool _isImplicit;

        protected CancellationToken _cancelToken;

        public AbstractWorker(/*ILog logCollector*/) : this(/*logCollector,*/ 0) { }

        public AbstractWorker(/*ILog logCollector,*/ int delayInMilliseconds, bool isImplicit = false)
        {
            //this._log = logCollector;
            this._delayInMilliseconds = delayInMilliseconds;
            this._isImplicit = isImplicit;
        }

        public Task Start(CancellationToken cancelToken)
        {
            var task = Task.Factory.StartNew(() => DoWork(cancelToken), cancelToken);
            return task;
        }

        /// <summary>
        /// Подготовка к регулярному выполнению операций.
        /// </summary>
        protected abstract void PrepareToWork();

        /// <summary>
        /// Выполнить единичную операцию.
        /// </summary>
        protected abstract void PerformOperation();

        /// <summary>
        /// Завершить работу.
        /// </summary>
        protected abstract void Stop();

        private void DoWork(CancellationToken cancelToken)
        {
            _cancelToken = cancelToken;
            /*
            if (_requestModerator == null)
            {
                _requestModerator = new RequestModerator(_delayInMilliseconds);
            }
            else
            {
                throw new InvalidOperationException("Handler is already started.");
            }
            */
            try
            {
                PrepareToWork();
            }
            catch (Exception exception)
            {
                //_log.Fatal("Unhandled exception caused during close.", exception);
                throw exception;
            }

            if (!_isImplicit)
            {
                //_log.Info("has been started to work.");
            }

            while (!_cancelToken.IsCancellationRequested)
            {
                try
                {
                    //_requestModerator.Wait();

                    PerformOperation();
                }
                catch (Exception exception)
                {
                    //_log.Error("Unhandled exception caused.", exception);
                }
                finally
                {
                    //_requestModerator.Done();
                }
            }

            StopAction();
        }

        private void StopAction()
        {
            try
            {
                Stop();
            }
            catch (Exception exception)
            {
                //_log.Warn("Unhandled exception caused during close.", exception);
            }
            finally
            {
                if (!_isImplicit)
                {
                    //_log.Info("stopped.");
                }
            }
        }
    }
}
