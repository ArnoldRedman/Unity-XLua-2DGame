---============================================================
--- Class.lua - 简易OOP实现
--- 
--- 用法:
---   local MyClass = Class("MyClass")
---   function MyClass:Ctor(name) self.name = name end
---   function MyClass:SayHello() print("Hello, " .. self.name) end
---
---   local obj = MyClass.New("World")
---   obj:SayHello()
---
--- 支持继承:
---   local Child = Class("Child", MyClass)
---============================================================

function Class(className, super)
    local cls = {}
    cls.__className = className
    cls.__index = cls

    if super then
        setmetatable(cls, { __index = super })
        cls.super = super
    end

    --- 创建实例
    function cls.New(...)
        local instance = setmetatable({}, cls)
        if instance.Ctor then
            instance:Ctor(...)
        end
        return instance
    end

    return cls
end

return Class
