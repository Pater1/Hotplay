using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Hotplay.Common.FileChain {
    [Flags]
    public enum RequestStatus: long {
        Open = 0,
        Locked = 1 << 0,
        Cached = 1 << 1,
        PredicitiveRequest = 1 << 2,

        IgnoreCache = Cached | PredicitiveRequest
    }
    public static class RequestLockManager {
        public static bool Is(this RequestStatus r1, RequestStatus r2 = ~RequestStatus.Open){
            return (r1 & r2) != RequestStatus.Open;
        }
        public static bool Is(this (string key, RequestStatus status) chained, RequestStatus status = ~RequestStatus.Open) {
            return chained.status.Is(status);
        }
        private static Dictionary<string, RequestStatus> Statuses = new Dictionary<string, RequestStatus>();
        public static RequestStatus GetStatus(string key) {
            RequestStatus ret;
            lock(Statuses) {
                if(Statuses.ContainsKey(key)) {
                    ret = Statuses[key];
                } else {
                    ret = RequestStatus.Open;
                }
            }
            return ret;
        }
        public static (string key, RequestStatus status) GetStatus_Chainable(string key) {
            return (key, GetStatus(key));
        }
        public static (string key, RequestStatus status) GetStatus(this (string key, RequestStatus status) chained) {
            return GetStatus_Chainable(chained.key);
        }
        public static RequestStatus SetStatus(string key, RequestStatus status) {
            lock(Statuses) {
                if(Statuses.ContainsKey(key)) {
                    Statuses[key] = status;
                } else {
                    Statuses.Add(key, status);
                }
            }
            return status;
        }
        public static (string key, RequestStatus status) SetStatus(this (string key, RequestStatus status) chained, RequestStatus status) {
            return (chained.key, SetStatus(chained.key, status));
        }
        public static (string key, RequestStatus status) AddStatus(string key, RequestStatus status) {
            if(status == RequestStatus.Locked) {
                LockAndWait(key);
                return (key, GetStatus(key));
            } else {
                RequestStatus s = GetStatus(key);
                s |= status;
                SetStatus(key, s);
                return (key, s);
            }
        }
        public static (string key, RequestStatus status) AddStatus(this (string key, RequestStatus status) chained, RequestStatus status) {
            return AddStatus(chained.key, status);
        }
        public static (string key, RequestStatus status) RemoveStatus(string key, RequestStatus status) {
            RequestStatus s = GetStatus(key);
            s &= ~status;
            SetStatus(key, s);
            return (key, s);
        }
        public static (string key, RequestStatus status) RemoveStatus(this (string key, RequestStatus status) chained, RequestStatus status) {
            return RemoveStatus(chained.key, status);
        }

        private static Dictionary<string, Semaphore> Semaphores = new Dictionary<string, Semaphore>();
        public static async Task LockAndWaitAsync(string key) {
            key = string.Intern(key);
            Semaphore s = Semaphores[key];
            s.WaitOne();
            while(LockAndWait_Internal(key)) {
                await Task.Yield();
            }
        }
        public static void LockAndWait(string key) {
            Semaphore s;
            lock(Semaphores) {
                if(Semaphores.ContainsKey(key)) {
                    s = Semaphores[key];
                } else {
                    s = new Semaphore(1,1);
                    Semaphores.Add(key, s);
                }
            }
            s.WaitOne();

            key = string.Intern(key);
            lock(key) {
                RequestStatus requestStatus = GetStatus(key);
                requestStatus |= RequestStatus.Locked;
                SetStatus(key, requestStatus);
            }
        }
        private static bool LockAndWait_Internal(string key) {
            RequestStatus requestStatus;
            lock(key) {
                bool locked;
                requestStatus = GetStatus(key);
                locked = requestStatus.Is(RequestStatus.Locked);

                if(locked) {
                    return true;
                } else {
                    requestStatus |= RequestStatus.Locked;
                    SetStatus(key, requestStatus);
                    return false;
                }
            }
        }
        public static (string key, RequestStatus status) LockAndWait(this (string key, RequestStatus status) chained) {
            LockAndWait(chained.key);
            return (chained.key, GetStatus(chained.key));
        }
        public static void Unlock(string key) {
            Semaphore s;
            lock(Semaphores) {
                if(Semaphores.ContainsKey(key)) {
                    s = Semaphores[key];
                } else {
                    s = new Semaphore(1,1);
                    Semaphores.Add(key, s);
                }
            }
            s.Release();

            key = string.Intern(key);
            lock(key) {
                RequestStatus requestStatus = GetStatus(key);
                requestStatus &= ~RequestStatus.Locked;
                SetStatus(key, requestStatus);
            }
        }
        public static (string key, RequestStatus status) Unlock(this (string key, RequestStatus status) chained) {
            Unlock(chained.key);
            return (chained.key, GetStatus(chained.key));
        }
    }
}
