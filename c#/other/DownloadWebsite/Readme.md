## fxxk
多线程实现有个死锁问题。`Cancel`时如果最终调用了`lock(xxx)`会发生死锁。
没找到问题。
排除的几个问题
1. `lock(this)`和`lock(m_lock_obj)`效果一样