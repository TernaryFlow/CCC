/**
 * CSharpWorkers Runtime Library
 * Lightweight runtime for executing C#-compiled JavaScript on Cloudflare Workers
 * Target: ES2025+
 */

// ============================================
// System.Object Base
// ============================================

class SystemObject {
    constructor() {
        this[Symbol.toStringTag] = 'SystemObject';
    }

    toString() {
        return this.constructor.name;
    }

    equals(other) {
        return this === other;
    }

    hashCode() {
        // Simple hash based on object identity
        return Symbol.keyFor?.(Symbol.for(this.toString())) || 0;
    }
}

// ============================================
// String Helpers
// ============================================

const StringHelpers = {
    isNullOrEmpty(s) {
        return s === null || s === undefined || s === '';
    },

    isNullOrWhiteSpace(s) {
        if (s === null || s === undefined) return true;
        return /^\s*$/.test(s);
    },

    format(template, ...args) {
        return template.replace(/{(\d+)}/g, (match, index) => {
            return typeof args[index] !== 'undefined' ? args[index] : match;
        });
    },

    concat(...strings) {
        return strings.filter(s => s !== null && s !== undefined).join('');
    },

    join(separator, items) {
        return Array.from(items).join(separator);
    }
};

// ============================================
// Equality and HashCode
// ============================================

const EqualityComparer = {
    default: {
        equals(a, b) {
            if (a === null && b === null) return true;
            if (a === null || b === null) return false;
            if (typeof a.equals === 'function') return a.equals(b);
            return a === b;
        },
        hashCode(obj) {
            if (obj === null) return 0;
            if (typeof obj.hashCode === 'function') return obj.hashCode();
            if (typeof obj === 'string') {
                let hash = 0;
                for (let i = 0; i < obj.length; i++) {
                    const char = obj.charCodeAt(i);
                    hash = ((hash << 5) - hash) + char;
                    hash = hash & hash;
                }
                return hash;
            }
            return Object.is(obj, obj) ? 1 : 0;
        }
    }
};

const HashCode = {
    start(seed = 0) {
        return new HashCodeBuilder(seed);
    }
};

class HashCodeBuilder {
    constructor(seed = 0) {
        this.hash = seed;
    }

    add(value) {
        const hash = EqualityComparer.default.hashCode(value);
        this.hash = ((this.hash << 5) + this.hash) ^ hash;
        return this;
    }

    toHashCode() {
        return this.hash;
    }
}

// ============================================
// List<T>
// ============================================

class List extends Array {
    constructor(...items) {
        super(...items);
    }

    static from(iterable) {
        return new List(...Array.from(iterable));
    }

    add(item) {
        this.push(item);
    }

    addRange(items) {
        for (const item of items) {
            this.push(item);
        }
    }

    insert(index, item) {
        this.splice(index, 0, item);
    }

    remove(item) {
        const index = this.indexOf(item);
        if (index > -1) {
            this.splice(index, 1);
            return true;
        }
        return false;
    }

    removeAll(predicate) {
        const initialLength = this.length;
        for (let i = this.length - 1; i >= 0; i--) {
            if (predicate(this[i])) {
                this.splice(i, 1);
            }
        }
        return initialLength - this.length;
    }

    contains(item) {
        return this.includes(item);
    }

    find(predicate) {
        return this.find(predicate) ?? null;
    }

    findAll(predicate) {
        return new List(...this.filter(predicate));
    }

    findIndex(predicate) {
        return this.findIndex(predicate);
    }

    forEach(action) {
        for (let i = 0; i < this.length; i++) {
            action(this[i]);
        }
    }

    getRange(index, count) {
        return new List(...this.slice(index, index + count));
    }

    indexOf(item) {
        return this.indexOf(item);
    }

    lastIndexOf(item) {
        return this.lastIndexOf(item);
    }

    reverse() {
        super.reverse();
        return this;
    }

    sort(comparator) {
        if (comparator) {
            super.sort((a, b) => {
                const result = comparator(a, b);
                return typeof result === 'number' ? result : 0;
            });
        } else {
            super.sort();
        }
        return this;
    }

    toArray() {
        return Array.from(this);
    }

    clear() {
        this.length = 0;
    }
}

// ============================================
// Dictionary<TKey, TValue>
// ============================================

class Dictionary {
    constructor(initialCapacityOrComparer) {
        this._map = new Map();
        this._keys = [];
    }

    static from(entries) {
        const dict = new Dictionary();
        for (const [key, value] of entries) {
            dict.set(key, value);
        }
        return dict;
    }

    get count() {
        return this._map.size;
    }

    get keys() {
        return new List(...this._map.keys());
    }

    get values() {
        return new List(...this._map.values());
    }

    get(key) {
        return this._map.get(key) ?? null;
    }

    set(key, value) {
        if (!this._map.has(key)) {
            this._keys.push(key);
        }
        this._map.set(key, value);
    }

    has(key) {
        return this._map.has(key);
    }

    delete(key) {
        return this._map.delete(key);
    }

    clear() {
        this._map.clear();
        this._keys = [];
    }

    forEach(action) {
        for (const [key, value] of this._map) {
            action(key, value);
        }
    }

    tryGetValue(key) {
        if (this._map.has(key)) {
            return { found: true, value: this._map.get(key) };
        }
        return { found: false, value: null };
    }

    *[Symbol.iterator]() {
        for (const [key, value] of this._map) {
            yield { key, value };
        }
    }

    entries() {
        return this._map.entries();
    }
}

// ============================================
// Queue<T>
// ============================================

class Queue {
    constructor(...items) {
        this._array = [...items];
    }

    get count() {
        return this._array.length;
    }

    enqueue(item) {
        this._array.push(item);
    }

    dequeue() {
        if (this._array.length === 0) {
            throw new Error('Queue is empty');
        }
        return this._array.shift();
    }

    peek() {
        if (this._array.length === 0) {
            throw new Error('Queue is empty');
        }
        return this._array[0];
    }

    clear() {
        this._array = [];
    }

    contains(item) {
        return this._array.includes(item);
    }

    toArray() {
        return [...this._array];
    }

    *[Symbol.iterator]() {
        for (const item of this._array) {
            yield item;
        }
    }
}

// ============================================
// Stack<T>
// ============================================

class Stack {
    constructor(...items) {
        this._array = [...items];
    }

    get count() {
        return this._array.length;
    }

    push(item) {
        this._array.push(item);
    }

    pop() {
        if (this._array.length === 0) {
            throw new Error('Stack is empty');
        }
        return this._array.pop();
    }

    peek() {
        if (this._array.length === 0) {
            throw new Error('Stack is empty');
        }
        return this._array[this._array.length - 1];
    }

    clear() {
        this._array = [];
    }

    contains(item) {
        return this._array.includes(item);
    }

    toArray() {
        return [...this._array];
    }

    *[Symbol.iterator]() {
        for (let i = this._array.length - 1; i >= 0; i--) {
            yield this._array[i];
        }
    }
}

// ============================================
// IEnumerable / IEnumerator
// ============================================

const Enumerable = {
    empty() {
        return [];
    },

    range(start, count) {
        return Array.from({ length: count }, (_, i) => start + i);
    },

    repeat(element, count) {
        return Array.from({ length: count }, () => element);
    },

    *generate(factory, count) {
        for (let i = 0; i < count; i++) {
            yield factory(i);
        }
    }
};

// ============================================
// Task and async helpers
// ============================================

class Task {
    constructor(executor) {
        this._promise = new Promise(executor);
    }

    static completed(result) {
        return Promise.resolve(result);
    }

    static rejected(error) {
        return Promise.reject(error);
    }

    static delay(milliseconds) {
        return new Promise(resolve => setTimeout(resolve, milliseconds));
    }

    static all(...tasks) {
        return Promise.all(tasks);
    }

    static race(...tasks) {
        return Promise.race(tasks);
    }

    then(onFulfilled, onRejected) {
        return this._promise.then(onFulfilled, onRejected);
    }

    catch(onRejected) {
        return this._promise.catch(onRejected);
    }

    finally(onFinally) {
        return this._promise.finally(onFinally);
    }

    getResult() {
        return this._promise;
    }

    get isCompleted() {
        return false; // Simplified - real impl would track state
    }

    get isCompletedSuccessfully() {
        return false;
    }

    get isFaulted() {
        return false;
    }

    get isCanceled() {
        return false;
    }
}

class CancellationToken {
    constructor() {
        this._isCancellationRequested = false;
        this._callbacks = [];
    }

    static get none() {
        return new CancellationToken();
    }

    get isCancellationRequested() {
        return this._isCancellationRequested;
    }

    get canBeCanceled() {
        return true;
    }

    cancel() {
        this._isCancellationRequested = true;
        for (const callback of this._callbacks) {
            callback();
        }
    }

    register(callback) {
        this._callbacks.push(callback);
        return {
            dispose: () => {
                const index = this._callbacks.indexOf(callback);
                if (index > -1) this._callbacks.splice(index, 1);
            }
        };
    }

    throwIfCancellationRequested() {
        if (this._isCancellationRequested) {
            throw new OperationCanceledException('The operation was canceled.');
        }
    }
}

class OperationCanceledException extends Error {
    constructor(message = 'The operation was canceled.') {
        super(message);
        this.name = 'OperationCanceledException';
    }
}

class TaskCanceledException extends OperationCanceledException {
    constructor(message = 'A task was canceled.') {
        super(message);
        this.name = 'TaskCanceledException';
    }
}

// ============================================
// DateTime
// ============================================

class DateTime {
    constructor(year, month, day, hour = 0, minute = 0, second = 0, millisecond = 0) {
        this._date = new Date(Date.UTC(year, month - 1, day, hour, minute, second, millisecond));
    }

    static get now() {
        return new DateTimeWrapper(new Date());
    }

    static get utcNow() {
        return new DateTimeWrapper(new Date());
    }

    static today() {
        const now = new Date();
        return new DateTimeWrapper(new Date(now.getFullYear(), now.getMonth(), now.getDate()));
    }

    static fromJsDate(date) {
        return new DateTimeWrapper(date);
    }

    get year() {
        return this._date.getUTCFullYear();
    }

    get month() {
        return this._date.getUTCMonth() + 1;
    }

    get day() {
        return this._date.getUTCDate();
    }

    get hour() {
        return this._date.getUTCHours();
    }

    get minute() {
        return this._date.getUTCMinutes();
    }

    get second() {
        return this._date.getUTCSeconds();
    }

    get millisecond() {
        return this._date.getUTCMilliseconds();
    }

    get dayOfWeek() {
        return this._date.getUTCDay();
    }

    get date() {
        return this._date;
    }

    toUniversalTime() {
        return new DateTimeWrapper(new Date(this._date.getTime()));
    }

    toLocalTime() {
        return new DateTimeWrapper(new Date(this._date.getTime()));
    }

    addDays(days) {
        return new DateTimeWrapper(new Date(this._date.getTime() + days * 86400000));
    }

    addHours(hours) {
        return new DateTimeWrapper(new Date(this._date.getTime() + hours * 3600000));
    }

    addMinutes(minutes) {
        return new DateTimeWrapper(new Date(this._date.getTime() + minutes * 60000));
    }

    addSeconds(seconds) {
        return new DateTimeWrapper(new Date(this._date.getTime() + seconds * 1000));
    }

    subtract(date) {
        return this._date.getTime() - date._date.getTime();
    }

    compareTo(other) {
        const diff = this._date.getTime() - other._date.getTime();
        return diff > 0 ? 1 : diff < 0 ? -1 : 0;
    }

    equals(other) {
        return this._date.getTime() === other._date.getTime();
    }

    toString(format) {
        if (!format) return this._date.toISOString();
        // Simple format support - extend as needed
        return this._date.toISOString();
    }

    toDateOnlyString() {
        return this._date.toISOString().split('T')[0];
    }

    toTimeOnlyString() {
        return this._date.toISOString().split('T')[1].split('.')[0];
    }
}

class DateTimeWrapper extends DateTime {
    constructor(date) {
        super(1970, 1, 1);
        this._date = date;
    }
}

// ============================================
// Guid
// ============================================

class Guid {
    constructor(value) {
        if (value) {
            this._value = value.toLowerCase();
        } else {
            this._value = Guid._generate();
        }
    }

    static _generate() {
        return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, c => {
            const r = Math.random() * 16 | 0;
            const v = c === 'x' ? r : (r & 0x3 | 0x8);
            return v.toString(16);
        });
    }

    static get empty() {
        return new Guid('00000000-0000-0000-0000-000000000000');
    }

    static parse(value) {
        return new Guid(value);
    }

    static tryParse(value) {
        try {
            return { success: true, result: new Guid(value) };
        } catch {
            return { success: false, result: Guid.empty };
        }
    }

    static newGuid() {
        return new Guid();
    }

    get value() {
        return this._value;
    }

    equals(other) {
        if (!(other instanceof Guid)) return false;
        return this._value === other._value;
    }

    hashCode() {
        return EqualityComparer.default.hashCode(this._value);
    }

    toString() {
        return this._value;
    }

    toString(format) {
        if (!format || format === 'D') return this._value;
        if (format === 'N') return this._value.replace(/-/g, '');
        if (format === 'B') return `{${this._value}}`;
        if (format === 'P') return `(${this._value})`;
        return this._value;
    }

    toByteArray() {
        // Convert GUID to byte array (little-endian)
        const parts = this._value.split('-');
        const bytes = [];
        
        // First three parts are little-endian
        for (const part of parts.slice(0, 3)) {
            for (let i = part.length - 2; i >= 0; i -= 2) {
                bytes.push(parseInt(part.substr(i, 2), 16));
            }
        }
        
        // Last two parts are big-endian
        for (const part of parts.slice(3)) {
            for (let i = 0; i < part.length; i += 2) {
                bytes.push(parseInt(part.substr(i, 2), 16));
            }
        }
        
        return bytes;
    }
}

// ============================================
// JSON Serialization
// ============================================

const JsonSerializer = {
    serialize(obj, options) {
        return JSON.stringify(obj, options?.replacer, options?.space);
    },

    deserialize(json, type) {
        const obj = JSON.parse(json);
        return obj;
    },

    serializeToUtf8(obj) {
        const json = JSON.stringify(obj);
        return new TextEncoder().encode(json);
    },

    deserializeFromUtf8(bytes, type) {
        const json = new TextDecoder().decode(bytes);
        return JSON.parse(json);
    }
};

// ============================================
// Exception Base Classes
// ============================================

class Exception extends Error {
    constructor(message, innerException) {
        super(message);
        this.name = 'Exception';
        this.innerException = innerException || null;
    }

    toString() {
        return `${this.name}: ${this.message}`;
    }
}

class SystemException extends Exception {
    constructor(message, innerException) {
        super(message, innerException);
        this.name = 'SystemException';
    }
}

class ArgumentException extends SystemException {
    constructor(message, paramName, innerException) {
        super(message, innerException);
        this.name = 'ArgumentException';
        this.paramName = paramName;
    }
}

class ArgumentNullException extends ArgumentException {
    constructor(paramName, message) {
        super(message || `Value cannot be null. (Parameter '${paramName}')`, paramName);
        this.name = 'ArgumentNullException';
    }
}

class ArgumentOutOfRangeException extends ArgumentException {
    constructor(paramName, actualValue, message) {
        super(message || `Specified argument was out of the range of valid values. (Parameter '${paramName}')`, paramName);
        this.name = 'ArgumentOutOfRangeException';
        this.actualValue = actualValue;
    }
}

class InvalidOperationException extends SystemException {
    constructor(message, innerException) {
        super(message, innerException);
        this.name = 'InvalidOperationException';
    }
}

class NotImplementedException extends SystemException {
    constructor(message) {
        super(message || 'The method or operation is not implemented.');
        this.name = 'NotImplementedException';
    }
}

class NullReferenceException extends SystemException {
    constructor(message) {
        super(message || 'Object reference not set to an instance of an object.');
        this.name = 'NullReferenceException';
    }
}

class IndexOutOfRangeException extends SystemException {
    constructor(message) {
        super(message || 'Index was outside the bounds of the array.');
        this.name = 'IndexOutOfRangeException';
    }
}

class KeyNotFoundException extends SystemException {
    constructor(message) {
        super(message || 'The given key was not present in the dictionary.');
        this.name = 'KeyNotFoundException';
    }
}

// ============================================
// Exports
// ============================================

export {
    SystemObject,
    StringHelpers,
    EqualityComparer,
    HashCode,
    HashCodeBuilder,
    List,
    Dictionary,
    Queue,
    Stack,
    Enumerable,
    Task,
    CancellationToken,
    OperationCanceledException,
    TaskCanceledException,
    DateTime,
    DateTimeWrapper,
    Guid,
    JsonSerializer,
    Exception,
    SystemException,
    ArgumentException,
    ArgumentNullException,
    ArgumentOutOfRangeException,
    InvalidOperationException,
    NotImplementedException,
    NullReferenceException,
    IndexOutOfRangeException,
    KeyNotFoundException
};
