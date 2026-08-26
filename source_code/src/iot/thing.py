# Source Generated with Decompyle++
# File: thing.pyc (Python 3.12)

import inspect
import json
from typing import Any, Callable, Dict, List

class ValueType:
    BOOLEAN = 'boolean'
    NUMBER = 'number'
    STRING = 'string'
    FLOAT = 'float'
    ARRAY = 'array'
    OBJECT = 'object'
    LIST = 'array'


class Property:
    
    def __init__(self = None, name = None, description = None, getter = ('name', str, 'description', str, 'getter', Callable)):
        self.name = name
        self.description = description
        self.getter = getter
        if not inspect.iscoroutinefunction(getter):
            raise TypeError(f'''Property getter của \'{name}\' phải là async function.''')
        self.type = ValueType.STRING
        self._type_determined = False

    
    def _determine_type(self = None, value = None):
        '''
        Xác định loại dữ liệu của property dựa trên giá trị.
        '''
        if isinstance(value, bool):
            self.type = ValueType.BOOLEAN
            return None
        if isinstance(value, int):
            self.type = ValueType.NUMBER
            return None
        if isinstance(value, float):
            self.type = ValueType.FLOAT
            return None
        if isinstance(value, str):
            self.type = ValueType.STRING
            return None
        if isinstance(value, (list, tuple)):
            self.type = ValueType.ARRAY
            return None
        if isinstance(value, dict):
            self.type = ValueType.OBJECT
            return None
        raise TypeError(f'''Loại property không được hỗ trợ: {type(value)}''')

    
    def get_descriptor_json(self = None):
        return {
            'description': self.description,
            'type': self.type }

    
    async def get_state_value(self):
        '''
        Lấy giá trị hiện tại của property.
        '''
        pass
    # WARNING: Decompyle incomplete



class Parameter:
    
    def __init__(self = None, name = None, description = None, type_ = (True,), required = ('name', str, 'description', str, 'type_', str, 'required', bool)):
        self.name = name
        self.description = description
        self.type = type_
        self.required = required
        self.value = None

    
    def get_descriptor_json(self = None):
        return {
            'description': self.description,
            'type': self.type }

    
    def set_value(self = None, value = None):
        self.value = value

    
    def get_value(self = None):
        return self.value



class Method:
    
    def __init__(self, name = None, description = None, parameters = None, callback = ('name', str, 'description', str, 'parameters', List[Parameter], 'callback', Callable)):
        self.name = name
        self.description = description
    # WARNING: Decompyle incomplete

    
    def get_descriptor_json(self = None):
        pass
    # WARNING: Decompyle incomplete

    
    async def invoke(self = None, params = None):
        '''
        Gọi method.
        '''
        pass
    # WARNING: Decompyle incomplete



class Thing:
    
    def __init__(self = None, name = None, description = None):
        self.name = name
        self.description = description
        self.properties = { }
        self.methods = { }

    
    def add_property(self = None, name = None, description = None, getter = ('name', str, 'description', str, 'getter', Callable, 'return', None)):
        self.properties[name] = Property(name, description, getter)

    
    def add_method(self, name = None, description = None, parameters = None, callback = ('name', str, 'description', str, 'parameters', List[Parameter], 'callback', Callable, 'return', None)):
        self.methods[name] = Method(name, description, parameters, callback)

    
    def get_descriptor_json(self = None):
        pass
    # WARNING: Decompyle incomplete

    
    async def get_state_json(self = None):
        '''
        Lấy trạng thái hiện tại của thiết bị.
        '''
        pass
    # WARNING: Decompyle incomplete

    
    async def invoke(self = None, command = None):
        '''
        Gọi method của thiết bị.
        '''
        pass
    # WARNING: Decompyle incomplete


