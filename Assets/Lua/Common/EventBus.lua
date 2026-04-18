---============================================================
--- EventBus.lua - 全局事件总线
---
--- 用法:
---   EventBus:On("BagChanged", handler, target)
---   EventBus:Off("BagChanged", handler, target)
---   EventBus:Emit("BagChanged", arg1, arg2)
---============================================================

local EventBus = {}
EventBus._listeners = {}

--- 注册事件监听
--- @param eventName string 事件名
--- @param handler function 回调函数
--- @param target table|nil 回调目标（用于移除时匹配）
function EventBus:On(eventName, handler, target)
    if not self._listeners[eventName] then
        self._listeners[eventName] = {}
    end
    table.insert(self._listeners[eventName], {
        handler = handler,
        target = target,
    })
end

--- 移除事件监听
--- @param eventName string 事件名
--- @param handler function 回调函数
--- @param target table|nil 回调目标
function EventBus:Off(eventName, handler, target)
    local list = self._listeners[eventName]
    if not list then return end

    for i = #list, 1, -1 do
        local item = list[i]
        if item.handler == handler and item.target == target then
            table.remove(list, i)
            break
        end
    end
end

--- 移除某个目标的所有事件监听
--- @param target table 目标对象
function EventBus:OffAll(target)
    for eventName, list in pairs(self._listeners) do
        for i = #list, 1, -1 do
            if list[i].target == target then
                table.remove(list, i)
            end
        end
    end
end

--- 触发事件
--- @param eventName string 事件名
function EventBus:Emit(eventName, ...)
    local list = self._listeners[eventName]
    if not list then return end

    for i = 1, #list do
        local item = list[i]
        if item.target then
            item.handler(item.target, ...)
        else
            item.handler(...)
        end
    end
end

--- 清除所有事件
function EventBus:Clear()
    self._listeners = {}
end

-- 挂到全局
_G.EventBus = EventBus

return EventBus
