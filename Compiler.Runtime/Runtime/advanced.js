/**
 * Advanced Features Runtime for C# to Cloudflare Workers Compiler
 * 
 * Provides runtime support for:
 * - Limited Reflection
 * - Dynamic binding simulation
 * - Expression tree evaluation
 * - Unsafe code simulation
 * - Threading primitives (async-based)
 * - Marshal operations
 * 
 * Note: These features have limited support in JavaScript environment
 */

// ==================== REFLECTION SUPPORT ====================

/**
 * Limited reflection system - type names only
 */
class Type {
    constructor(name, fullName, namespace_, baseType = null) {
        this.name = name;
        this.fullName = fullName;
        this.namespace = namespace_;
        this.baseType = baseType;
        this._properties = new Map();
        this._methods = new Map();
        this._fields = new Map();
    }

    static fromConstructor(ctor) {
        const name = ctor.name || 'Anonymous';
        return new Type(name, name, '');
    }

    getName() {
        return this.name;
    }

    getFullName() {
        return this.fullName;
    }

    getNamespace() {
        return this.namespace;
    }

    getBaseType() {
        return this.baseType;
    }

    addProperty(name, type) {
        this._properties.set(name, type);
        return this;
    }

    addMethod(name, returnType, params_) {
        this._methods.set(name, { returnType, params: params_ || [] });
        return this;
    }

    addField(name, type) {
        this._fields.set(name, type);
        return this;
    }

    getProperties() {
        return Array.from(this._properties.entries()).map(([name, type]) => ({
            name,
            propertyType: type,
            getValue: (obj) => obj[name]
        }));
    }

    getMethods() {
        return Array.from(this._methods.entries()).map(([name, info]) => ({
            name,
            returnType: info.returnType,
            parameters: info.params
        }));
    }

    getFields() {
        return Array.from(this._fields.entries()).map(([name, type]) => ({
            name,
            fieldType: type
        }));
    }

    getProperty(name) {
        const prop = this._properties.get(name);
        if (!prop) return null;
        return {
            name,
            propertyType: prop,
            canRead: true,
            canWrite: true,
            getValue: (obj) => obj[name],
            setValue: (obj, value) => { obj[name] = value; }
        };
    }

    getMethod(name, paramTypes = []) {
        const method = this._methods.get(name);
        if (!method) return null;
        return {
            name,
            returnType: method.returnType,
            parameters: method.params,
            invoke: (obj, args) => obj[name](...args)
        };
    }

    toString() {
        return this.fullName;
    }
}

/**
 * BindingFlags enum simulation
 */
const BindingFlags = {
    Default: 0,
    IgnoreCase: 1,
    DeclaredOnly: 2,
    Instance: 4,
    Static: 8,
    Public: 16,
    NonPublic: 32,
    FlattenHierarchy: 64
};

/**
 * Limited reflection helper
 */
class ReflectionHelper {
    static getTypeName(obj) {
        if (obj === null || obj === undefined) {
            return 'null';
        }
        if (typeof obj === 'object' && obj.constructor) {
            return obj.constructor.name || 'Object';
        }
        return typeof obj;
    }

    static getTypeFullName(obj) {
        if (obj === null || obj === undefined) {
            return 'System.Null';
        }
        if (typeof obj === 'object' && obj.constructor) {
            return obj.constructor.name || 'System.Object';
        }
        return `System.${this.capitalize(typeof obj)}`;
    }

    static getProperties(obj, flags = BindingFlags.Public | BindingFlags.Instance) {
        if (!obj || typeof obj !== 'object') {
            return [];
        }
        
        const props = Object.getOwnPropertyNames(obj);
        return props.filter(name => {
            const descriptor = Object.getOwnPropertyDescriptor(obj, name);
            if (!descriptor) return false;
            if (flags & BindingFlags.Instance && !descriptor.writable && !descriptor.get) return false;
            return true;
        }).map(name => ({
            name,
            value: obj[name],
            type: typeof obj[name]
        }));
    }

    static getProperty(obj, name) {
        if (!obj || typeof obj !== 'object') {
            throw new Error(`Cannot get property '${name}' from ${obj}`);
        }
        return obj[name];
    }

    static setProperty(obj, name, value) {
        if (!obj || typeof obj !== 'object') {
            throw new Error(`Cannot set property '${name}' on ${obj}`);
        }
        obj[name] = value;
    }

    static capitalize(str) {
        return str.charAt(0).toUpperCase() + str.slice(1);
    }
}

// ==================== DYNAMIC SUPPORT ====================

/**
 * Dynamic object wrapper for runtime binding simulation
 */
class DynamicObject {
    constructor(target = {}) {
        this.$target = target;
        this.$cache = new Map();
    }

    static wrap(obj) {
        if (obj instanceof DynamicObject) {
            return obj;
        }
        return new DynamicObject(obj);
    }

    unwrap() {
        return this.$target;
    }

    getMember(name) {
        // Check cache first
        if (this.$cache.has(name)) {
            const cached = this.$cache.get(name);
            return typeof cached === 'function' ? cached.bind(this.$target) : cached;
        }

        // Try to get from target
        const value = this.$target[name];
        if (value !== undefined) {
            this.$cache.set(name, value);
            if (typeof value === 'function') {
                return value.bind(this.$target);
            }
            return value;
        }

        // Try method missing pattern
        if (this.$target.tryGetMember) {
            return this.$target.tryGetMember(name);
        }

        throw new ReferenceError(`Cannot get member '${name}' on dynamic object`);
    }

    setMember(name, value) {
        this.$target[name] = value;
        this.$cache.delete(name);
    }

    invokeMethod(name, args) {
        const method = this.getMember(name);
        if (typeof method !== 'function') {
            throw new TypeError(`Member '${name}' is not a method`);
        }
        return method(...args);
    }

    // Proxy handler for natural syntax
    static createProxy(target) {
        const dynamic = new DynamicObject(target);
        return new Proxy(target, {
            get: (t, prop) => {
                if (prop === '$target' || prop === '$cache') {
                    return dynamic[prop];
                }
                return dynamic.getMember(prop);
            },
            set: (t, prop, value) => {
                dynamic.setMember(prop, value);
                return true;
            },
            has: (t, prop) => {
                return prop in t || dynamic.$cache.has(prop);
            },
            deleteProperty: (t, prop) => {
                dynamic.$cache.delete(prop);
                return delete t[prop];
            }
        });
    }
}

/**
 * Dynamic binder simulation
 */
class Binder {
    static getMember(name) {
        return { type: 'GetMember', name };
    }

    static setMember(name) {
        return { type: 'SetMember', name };
    }

    static invokeMember(name, args) {
        return { type: 'InvokeMember', name, args };
    }
}

// ==================== EXPRESSION TREES ====================

/**
 * Expression tree node types
 */
const ExpressionType = {
    Add: 'Add',
    Subtract: 'Subtract',
    Multiply: 'Multiply',
    Divide: 'Divide',
    Modulo: 'Modulo',
    Equal: 'Equal',
    NotEqual: 'NotEqual',
    LessThan: 'LessThan',
    LessThanOrEqual: 'LessThanOrEqual',
    GreaterThan: 'GreaterThan',
    GreaterThanOrEqual: 'GreaterThanOrEqual',
    And: 'And',
    Or: 'Or',
    Not: 'Not',
    Negate: 'Negate',
    Parameter: 'Parameter',
    Constant: 'Constant',
    MemberAccess: 'MemberAccess',
    MethodCall: 'MethodCall',
    Lambda: 'Lambda',
    Invoke: 'Invoke',
    New: 'New',
    NewArrayInit: 'NewArrayInit',
    NewArrayBounds: 'NewArrayBounds',
    Call: 'Call',
    Convert: 'Convert',
    Coalesce: 'Coalesce',
    Conditional: 'Conditional',
    Block: 'Block',
    Assign: 'Assign'
};

/**
 * Base expression class
 */
class Expression {
    constructor(type, nodeType) {
        this.type = type;
        this.nodeType = nodeType;
    }

    static constant(value, type = null) {
        return new ConstantExpression(value, type);
    }

    static parameter(type, name) {
        return new ParameterExpression(type, name);
    }

    static add(left, right) {
        return new BinaryExpression(ExpressionType.Add, left, right);
    }

    static subtract(left, right) {
        return new BinaryExpression(ExpressionType.Subtract, left, right);
    }

    static multiply(left, right) {
        return new BinaryExpression(ExpressionType.Multiply, left, right);
    }

    static divide(left, right) {
        return new BinaryExpression(ExpressionType.Divide, left, right);
    }

    static equal(left, right) {
        return new BinaryExpression(ExpressionType.Equal, left, right);
    }

    static notEqual(left, right) {
        return new BinaryExpression(ExpressionType.NotEqual, left, right);
    }

    static lessThan(left, right) {
        return new BinaryExpression(ExpressionType.LessThan, left, right);
    }

    static greaterThan(left, right) {
        return new BinaryExpression(ExpressionType.GreaterThan, left, right);
    }

    static andAlso(left, right) {
        return new BinaryExpression(ExpressionType.And, left, right);
    }

    static orElse(left, right) {
        return new BinaryExpression(ExpressionType.Or, left, right);
    }

    static not(operand) {
        return new UnaryExpression(ExpressionType.Not, operand);
    }

    static negate(operand) {
        return new UnaryExpression(ExpressionType.Negate, operand);
    }

    static memberAccess(expression, member) {
        return new MemberExpression(expression, member);
    }

    static call(instance, method, ...arguments_) {
        return new MethodCallExpression(instance, method, arguments_);
    }

    static lambda(body, ...parameters) {
        return new LambdaExpression(body, parameters);
    }

    static condition(test, ifTrue, ifFalse) {
        return new ConditionalExpression(test, ifTrue, ifFalse);
    }

    static block(...expressions) {
        return new BlockExpression(expressions);
    }

    static assign(left, right) {
        return new BinaryExpression(ExpressionType.Assign, left, right);
    }

    compile() {
        throw new Error('compile() must be implemented by subclass');
    }
}

class ConstantExpression extends Expression {
    constructor(value, type = null) {
        super('Constant', ExpressionType.Constant);
        this.value = value;
        this.type = type;
    }

    compile() {
        return () => this.value;
    }
}

class ParameterExpression extends Expression {
    constructor(type, name) {
        super('Parameter', ExpressionType.Parameter);
        this.type = type;
        this.name = name;
    }

    compile(env = {}) {
        return () => {
            if (!(this.name in env)) {
                throw new Error(`Parameter '${this.name}' not found in environment`);
            }
            return env[this.name];
        };
    }
}

class BinaryExpression extends Expression {
    constructor(nodeType, left, right) {
        super('Binary', nodeType);
        this.left = left;
        this.right = right;
        this.nodeType = nodeType;
    }

    compile(env = {}) {
        const leftFn = this.left.compile(env);
        const rightFn = this.right.compile(env);

        switch (this.nodeType) {
            case ExpressionType.Add:
                return () => leftFn() + rightFn();
            case ExpressionType.Subtract:
                return () => leftFn() - rightFn();
            case ExpressionType.Multiply:
                return () => leftFn() * rightFn();
            case ExpressionType.Divide:
                return () => leftFn() / rightFn();
            case ExpressionType.Modulo:
                return () => leftFn() % rightFn();
            case ExpressionType.Equal:
                return () => leftFn() === rightFn();
            case ExpressionType.NotEqual:
                return () => leftFn() !== rightFn();
            case ExpressionType.LessThan:
                return () => leftFn() < rightFn();
            case ExpressionType.LessThanOrEqual:
                return () => leftFn() <= rightFn();
            case ExpressionType.GreaterThan:
                return () => leftFn() > rightFn();
            case ExpressionType.GreaterThanOrEqual:
                return () => leftFn() >= rightFn();
            case ExpressionType.And:
                return () => leftFn() && rightFn();
            case ExpressionType.Or:
                return () => leftFn() || rightFn();
            case ExpressionType.Assign:
                return () => {
                    // Assignment handled specially
                    return rightFn();
                };
            default:
                throw new Error(`Unsupported binary operation: ${this.nodeType}`);
        }
    }
}

class UnaryExpression extends Expression {
    constructor(nodeType, operand) {
        super('Unary', nodeType);
        this.operand = operand;
        this.nodeType = nodeType;
    }

    compile(env = {}) {
        const operandFn = this.operand.compile(env);

        switch (this.nodeType) {
            case ExpressionType.Not:
                return () => !operandFn();
            case ExpressionType.Negate:
                return () => -operandFn();
            default:
                throw new Error(`Unsupported unary operation: ${this.nodeType}`);
        }
    }
}

class MemberExpression extends Expression {
    constructor(expression, member) {
        super('MemberAccess', ExpressionType.MemberAccess);
        this.expression = expression;
        this.member = member;
    }

    compile(env = {}) {
        const exprFn = this.expression.compile(env);
        return () => {
            const obj = exprFn();
            return obj[this.member];
        };
    }
}

class MethodCallExpression extends Expression {
    constructor(instance, method, arguments_) {
        super('MethodCall', ExpressionType.MethodCall);
        this.instance = instance;
        this.method = method;
        this.arguments = arguments_;
    }

    compile(env = {}) {
        const instanceFn = this.instance ? this.instance.compile(env) : null;
        const argFns = this.arguments.map(arg => arg.compile(env));

        return () => {
            const instance = instanceFn ? instanceFn() : null;
            const args = argFns.map(fn => fn());
            
            if (typeof this.method === 'string') {
                return instance[this.method](...args);
            }
            return this.method(instance, ...args);
        };
    }
}

class LambdaExpression extends Expression {
    constructor(body, parameters) {
        super('Lambda', ExpressionType.Lambda);
        this.body = body;
        this.parameters = parameters;
    }

    compile(env = {}) {
        const paramNames = this.parameters.map(p => p.name);
        const bodyFn = this.body.compile(env);

        return (...args) => {
            const localEnv = { ...env };
            paramNames.forEach((name, i) => {
                localEnv[name] = args[i];
            });
            return this.body.compile(localEnv)();
        };
    }
}

class ConditionalExpression extends Expression {
    constructor(test, ifTrue, ifFalse) {
        super('Conditional', ExpressionType.Conditional);
        this.test = test;
        this.ifTrue = ifTrue;
        this.ifFalse = ifFalse;
    }

    compile(env = {}) {
        const testFn = this.test.compile(env);
        const trueFn = this.ifTrue.compile(env);
        const falseFn = this.ifFalse.compile(env);

        return () => testFn() ? trueFn() : falseFn();
    }
}

class BlockExpression extends Expression {
    constructor(expressions) {
        super('Block', ExpressionType.Block);
        this.expressions = expressions;
    }

    compile(env = {}) {
        const compiledExprs = this.expressions.map(expr => expr.compile(env));
        return () => {
            let result;
            for (const expr of compiledExprs) {
                result = expr();
            }
            return result;
        };
    }
}

/**
 * Expression visitor for transformation
 */
class ExpressionVisitor {
    visit(expr) {
        if (!expr) return null;

        switch (expr.type) {
            case 'Constant':
                return this.visitConstant(expr);
            case 'Parameter':
                return this.visitParameter(expr);
            case 'Binary':
                return this.visitBinary(expr);
            case 'Unary':
                return this.visitUnary(expr);
            case 'MemberAccess':
                return this.visitMemberAccess(expr);
            case 'MethodCall':
                return this.visitMethodCall(expr);
            case 'Lambda':
                return this.visitLambda(expr);
            case 'Conditional':
                return this.visitConditional(expr);
            case 'Block':
                return this.visitBlock(expr);
            default:
                return this.visitUnknown(expr);
        }
    }

    visitConstant(expr) { return expr; }
    visitParameter(expr) { return expr; }
    visitBinary(expr) {
        const left = this.visit(expr.left);
        const right = this.visit(expr.right);
        return left !== expr.left || right !== expr.right
            ? new BinaryExpression(expr.nodeType, left, right)
            : expr;
    }
    visitUnary(expr) {
        const operand = this.visit(expr.operand);
        return operand !== expr.operand
            ? new UnaryExpression(expr.nodeType, operand)
            : expr;
    }
    visitMemberAccess(expr) { return expr; }
    visitMethodCall(expr) { return expr; }
    visitLambda(expr) { return expr; }
    visitConditional(expr) { return expr; }
    visitBlock(expr) { return expr; }
    visitUnknown(expr) { return expr; }
}

// ==================== THREADING SIMULATION ====================

/**
 * Async-based threading primitives simulation
 */
class SemaphoreSlim {
    constructor(initialCount, maxCount = Number.MAX_SAFE_INTEGER) {
        this.count = initialCount;
        this.maxCount = maxCount;
        this.queue = [];
    }

    async wait(cancellationToken = null) {
        if (cancellationToken?.isCancellationRequested) {
            throw new OperationCanceledException('Operation was cancelled');
        }

        if (this.count > 0) {
            this.count--;
            return Promise.resolve(true);
        }

        return new Promise((resolve, reject) => {
            const waiter = { resolve, reject };
            this.queue.push(waiter);

            if (cancellationToken) {
                cancellationToken.register(() => {
                    const idx = this.queue.indexOf(waiter);
                    if (idx !== -1) {
                        this.queue.splice(idx, 1);
                        reject(new OperationCanceledException('Operation was cancelled'));
                    }
                });
            }
        });
    }

    release(releaseCount = 1) {
        for (let i = 0; i < releaseCount && this.queue.length > 0; i++) {
            const waiter = this.queue.shift();
            if (waiter) {
                this.count++;
                waiter.resolve(true);
            }
        }
        this.count = Math.min(this.count + releaseCount, this.maxCount);
    }
}

/**
 * Mutex simulation using SemaphoreSlim
 */
class Mutex {
    constructor() {
        this.semaphore = new SemaphoreSlim(1, 1);
        this.owner = null;
    }

    async waitOne(cancellationToken = null) {
        await this.semaphore.wait(cancellationToken);
        this.owner = Symbol('mutex-owner');
        return true;
    }

    releaseMutex() {
        this.owner = null;
        this.semaphore.release(1);
    }
}

/**
 * ManualResetEventSlim simulation
 */
class ManualResetEventSlim {
    constructor(initialState = false) {
        this.signaled = initialState;
        this.waiters = [];
    }

    async wait(cancellationToken = null, timeout = -1) {
        if (this.signaled) {
            return Promise.resolve(true);
        }

        if (cancellationToken?.isCancellationRequested) {
            throw new OperationCanceledException('Operation was cancelled');
        }

        return new Promise((resolve, reject) => {
            const waiter = { resolve, reject };
            this.waiters.push(waiter);

            if (cancellationToken) {
                cancellationToken.register(() => {
                    const idx = this.waiters.indexOf(waiter);
                    if (idx !== -1) {
                        this.waiters.splice(idx, 1);
                        reject(new OperationCanceledException('Operation was cancelled'));
                    }
                });
            }

            if (timeout > 0) {
                setTimeout(() => {
                    const idx = this.waiters.indexOf(waiter);
                    if (idx !== -1) {
                        this.waiters.splice(idx, 1);
                        resolve(false);
                    }
                }, timeout);
            }
        });
    }

    set() {
        this.signaled = true;
        const waiters = this.waiters;
        this.waiters = [];
        waiters.forEach(w => w.resolve(true));
    }

    reset() {
        this.signaled = false;
    }
}

/**
 * CancellationToken simulation
 */
class CancellationTokenSource {
    constructor() {
        this.isCancelled = false;
        this.callbacks = [];
        this.token = {
            isCancellationRequested: false,
            register: (callback) => {
                this.callbacks.push(callback);
                if (this.isCancelled) {
                    callback();
                }
                return { dispose: () => {} };
            }
        };
    }

    cancel() {
        if (this.isCancelled) return;
        this.isCancelled = true;
        this.token.isCancellationRequested = true;
        this.callbacks.forEach(cb => cb());
    }

    dispose() {
        this.callbacks = [];
    }
}

/**
 * OperationCanceledException
 */
class OperationCanceledException extends Error {
    constructor(message = 'The operation was canceled.') {
        super(message);
        this.name = 'OperationCanceledException';
    }
}

/**
 * Timeout exception
 */
class TimeoutException extends Error {
    constructor(message = 'The operation has timed out.') {
        super(message);
        this.name = 'TimeoutException';
    }
}

// ==================== MARSHAL SIMULATION ====================

/**
 * Marshal helper for binary data
 */
class MarshalSimulator {
    /**
     * Copy memory from source to destination
     */
    static copy(source, destination, startIndex, length) {
        if (source instanceof ArrayBuffer && destination instanceof ArrayBuffer) {
            const srcView = new Uint8Array(source);
            const dstView = new Uint8Array(destination);
            for (let i = 0; i < length; i++) {
                dstView[startIndex + i] = srcView[i];
            }
        } else if (ArrayBuffer.isView(source) && ArrayBuffer.isView(destination)) {
            for (let i = 0; i < length; i++) {
                destination[startIndex + i] = source[i];
            }
        } else {
            throw new Error('Marshal.copy requires ArrayBuffer or typed array views');
        }
    }

    /**
     * Allocate HGlobal equivalent (ArrayBuffer)
     */
    static allocHGlobal(size) {
        return new ArrayBuffer(size);
    }

    /**
     * Free HGlobal equivalent
     */
    static freeHGlobal(handle) {
        // In JavaScript, memory is garbage collected
        // This is a no-op but provided for API compatibility
        return;
    }

    /**
     * Get size of type (approximate)
     */
    static sizeOf(type) {
        const sizes = {
            'Int8': 1,
            'UInt8': 1,
            'Int16': 2,
            'UInt16': 2,
            'Int32': 4,
            'UInt32': 4,
            'Int64': 8,
            'UInt64': 8,
            'Single': 4,
            'Double': 8,
            'Char': 2,
            'Boolean': 1,
            'Pointer': 8
        };
        return sizes[type] || 8;
    }

    /**
     * Read integer from buffer
     */
    static readInt32(buffer, offset = 0) {
        const view = new DataView(buffer);
        return view.getInt32(offset, true); // little-endian
    }

    /**
     * Write integer to buffer
     */
    static writeInt32(buffer, offset, value) {
        const view = new DataView(buffer);
        view.setInt32(offset, value, true);
    }

    /**
     * Read string from buffer (UTF-8)
     */
    static ptrToStringAuto(ptr, len) {
        const bytes = new Uint8Array(ptr, 0, len);
        return new TextDecoder('utf-8').decode(bytes);
    }

    /**
     * Write string to buffer (UTF-8)
     */
    static stringToHGlobalAuto(str) {
        const encoder = new TextEncoder();
        const encoded = encoder.encode(str);
        const buffer = new ArrayBuffer(encoded.length + 1); // +1 for null terminator
        const view = new Uint8Array(buffer);
        view.set(encoded);
        return buffer;
    }

    /**
     * Structure to bytes
     */
    static structureToPtr(obj, structureSize) {
        const buffer = new ArrayBuffer(structureSize);
        const view = new DataView(buffer);
        
        let offset = 0;
        for (const [key, value] of Object.entries(obj)) {
            if (typeof value === 'number') {
                view.setFloat64(offset, value, true);
                offset += 8;
            } else if (typeof value === 'boolean') {
                view.setUint8(offset, value ? 1 : 0);
                offset += 1;
            }
        }
        
        return buffer;
    }

    /**
     * Bytes to structure
     */
    static ptrToStructure(buffer, ctor) {
        const view = new DataView(buffer);
        const obj = new ctor();
        
        let offset = 0;
        for (const key of Object.keys(obj)) {
            if (typeof obj[key] === 'number') {
                obj[key] = view.getFloat64(offset, true);
                offset += 8;
            } else if (typeof obj[key] === 'boolean') {
                obj[key] = view.getUint8(offset) !== 0;
                offset += 1;
            }
        }
        
        return obj;
    }
}

// ==================== EXPORTS ====================

if (typeof module !== 'undefined' && module.exports) {
    module.exports = {
        Type,
        BindingFlags,
        ReflectionHelper,
        DynamicObject,
        Binder,
        Expression,
        ExpressionType,
        ConstantExpression,
        ParameterExpression,
        BinaryExpression,
        UnaryExpression,
        MemberExpression,
        MethodCallExpression,
        LambdaExpression,
        ConditionalExpression,
        BlockExpression,
        ExpressionVisitor,
        SemaphoreSlim,
        Mutex,
        ManualResetEventSlim,
        CancellationTokenSource,
        OperationCanceledException,
        TimeoutException,
        MarshalSimulator
    };
}
