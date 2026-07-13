/**
 * LINQ Extension Methods
 * Compiled from C# LINQ to native JavaScript array methods
 */

import { List } from './runtime.js';

export const Linq = {
    // ============================================
    // Filtering
    // ============================================

    where(source, predicate) {
        if (!source) return new List();
        return new List(...Array.from(source).filter(predicate));
    },

    filter(source, predicate) {
        return this.where(source, predicate);
    },

    // ============================================
    // Projection
    // ============================================

    select(source, selector) {
        if (!source) return new List();
        return new List(...Array.from(source).map(selector));
    },

    map(source, selector) {
        return this.select(source, selector);
    },

    selectMany(source, collectionSelector, resultSelector) {
        if (!source) return new List();
        if (resultSelector) {
            return new List(...Array.from(source).flatMap(
                (item, index) => Array.from(collectionSelector(item, index)).map(
                    collectionItem => resultSelector(item, collectionItem)
                )
            ));
        }
        return new List(...Array.from(source).flatMap(collectionSelector));
    },

    // ============================================
    // Element Operations
    // ============================================

    first(source, predicate) {
        if (!source) throw new Error('Sequence contains no elements');
        const arr = Array.from(source);
        if (predicate) {
            const found = arr.find(predicate);
            if (found === undefined) throw new Error('Sequence contains no matching element');
            return found;
        }
        if (arr.length === 0) throw new Error('Sequence contains no elements');
        return arr[0];
    },

    firstOrDefault(source, predicate, defaultValue = null) {
        if (!source) return defaultValue;
        const arr = Array.from(source);
        if (predicate) {
            return arr.find(predicate) ?? defaultValue;
        }
        return arr.length > 0 ? arr[0] : defaultValue;
    },

    last(source, predicate) {
        if (!source) throw new Error('Sequence contains no elements');
        const arr = Array.from(source);
        if (predicate) {
            for (let i = arr.length - 1; i >= 0; i--) {
                if (predicate(arr[i])) return arr[i];
            }
            throw new Error('Sequence contains no matching element');
        }
        if (arr.length === 0) throw new Error('Sequence contains no elements');
        return arr[arr.length - 1];
    },

    lastOrDefault(source, predicate, defaultValue = null) {
        if (!source) return defaultValue;
        const arr = Array.from(source);
        if (predicate) {
            for (let i = arr.length - 1; i >= 0; i--) {
                if (predicate(arr[i])) return arr[i];
            }
            return defaultValue;
        }
        return arr.length > 0 ? arr[arr.length - 1] : defaultValue;
    },

    single(source, predicate) {
        if (!source) throw new Error('Sequence contains no elements');
        const arr = Array.from(source);
        const filtered = predicate ? arr.filter(predicate) : arr;
        if (filtered.length === 0) throw new Error('Sequence contains no matching element');
        if (filtered.length > 1) throw new Error('Sequence contains more than one matching element');
        return filtered[0];
    },

    singleOrDefault(source, predicate, defaultValue = null) {
        if (!source) return defaultValue;
        const arr = Array.from(source);
        const filtered = predicate ? arr.filter(predicate) : arr;
        if (filtered.length === 0) return defaultValue;
        if (filtered.length > 1) throw new Error('Sequence contains more than one matching element');
        return filtered[0];
    },

    elementAt(source, index) {
        if (!source) throw new Error('Index out of range');
        const arr = Array.from(source);
        if (index < 0 || index >= arr.length) throw new Error('Index out of range');
        return arr[index];
    },

    elementAtOrDefault(source, index, defaultValue = null) {
        if (!source) return defaultValue;
        const arr = Array.from(source);
        if (index < 0 || index >= arr.length) return defaultValue;
        return arr[index];
    },

    // ============================================
    // Aggregation
    // ============================================

    count(source, predicate) {
        if (!source) return 0;
        if (predicate) {
            return Array.from(source).filter(predicate).length;
        }
        return Array.from(source).length;
    },

    sum(source, selector) {
        if (!source) return 0;
        const arr = selector ? Array.from(source).map(selector) : Array.from(source);
        return arr.reduce((acc, val) => acc + (typeof val === 'number' ? val : 0), 0);
    },

    average(source, selector) {
        if (!source) return 0;
        const arr = selector ? Array.from(source).map(selector) : Array.from(source);
        if (arr.length === 0) return 0;
        return arr.reduce((acc, val) => acc + (typeof val === 'number' ? val : 0), 0) / arr.length;
    },

    min(source, selector) {
        if (!source) return null;
        const arr = selector ? Array.from(source).map(selector) : Array.from(source);
        if (arr.length === 0) return null;
        return Math.min(...arr);
    },

    max(source, selector) {
        if (!source) return null;
        const arr = selector ? Array.from(source).map(selector) : Array.from(source);
        if (arr.length === 0) return null;
        return Math.max(...arr);
    },

    aggregate(source, func, seed) {
        if (!source) return seed;
        const arr = Array.from(source);
        if (seed !== undefined) {
            return arr.reduce(func, seed);
        }
        if (arr.length === 0) return null;
        return arr.reduce(func);
    },

    // ============================================
    // Ordering
    // ============================================

    orderBy(source, keySelector, comparer) {
        if (!source) return new List();
        const arr = Array.from(source);
        if (comparer) {
            arr.sort((a, b) => comparer(keySelector(a), keySelector(b)));
        } else {
            arr.sort((a, b) => {
                const ka = keySelector(a);
                const kb = keySelector(b);
                if (ka < kb) return -1;
                if (ka > kb) return 1;
                return 0;
            });
        }
        return new List(...arr);
    },

    orderByDescending(source, keySelector, comparer) {
        if (!source) return new List();
        const arr = Array.from(source);
        if (comparer) {
            arr.sort((a, b) => comparer(keySelector(b), keySelector(a)));
        } else {
            arr.sort((a, b) => {
                const ka = keySelector(a);
                const kb = keySelector(b);
                if (ka < kb) return 1;
                if (ka > kb) return -1;
                return 0;
            });
        }
        return new List(...arr);
    },

    thenBy(source, keySelector, comparer) {
        // For chained ordering - simplified implementation
        return this.orderBy(source, keySelector, comparer);
    },

    thenByDescending(source, keySelector, comparer) {
        return this.orderByDescending(source, keySelector, comparer);
    },

    reverse(source) {
        if (!source) return new List();
        return new List(...Array.from(source).reverse());
    },

    // ============================================
    // Grouping
    // ============================================

    groupBy(source, keySelector, elementSelector, resultSelector) {
        if (!source) return new List();
        const groups = new Map();
        
        for (const item of source) {
            const key = keySelector(item);
            const element = elementSelector ? elementSelector(item) : item;
            
            if (!groups.has(key)) {
                groups.set(key, []);
            }
            groups.get(key).push(element);
        }

        if (resultSelector) {
            return new List(...Array.from(groups.entries()).map(
                ([key, elements]) => resultSelector(key, elements)
            ));
        }

        return new List(...Array.from(groups.entries()).map(
            ([key, elements]) => ({ key, elements: new List(...elements) })
        ));
    },

    // ============================================
    // Join and Set Operations
    // ============================================

    join(outer, inner, outerKeySelector, innerKeySelector, resultSelector) {
        if (!outer || !inner) return new List();
        const result = [];
        
        for (const outerItem of outer) {
            const outerKey = outerKeySelector(outerItem);
            for (const innerItem of inner) {
                const innerKey = innerKeySelector(innerItem);
                if (outerKey === innerKey) {
                    result.push(resultSelector(outerItem, innerItem));
                }
            }
        }
        
        return new List(...result);
    },

    concat(first, second) {
        if (!first) return new List(...(second || []));
        if (!second) return new List(...first);
        return new List(...first, ...second);
    },

    union(first, second, comparer) {
        if (!first && !second) return new List();
        const set = new Set(comparer ? [] : [...(first || []), ...(second || [])]);
        
        if (comparer) {
            const items = [...(first || []), ...(second || [])];
            const unique = [];
            for (const item of items) {
                if (!unique.some(u => comparer(u, item))) {
                    unique.push(item);
                }
            }
            return new List(...unique);
        }
        
        return new List(...set);
    },

    intersect(first, second, comparer) {
        if (!first || !second) return new List();
        
        if (comparer) {
            const result = [];
            for (const item of first) {
                if (second.some(s => comparer(s, item)) && !result.some(r => comparer(r, item))) {
                    result.push(item);
                }
            }
            return new List(...result);
        }
        
        const secondSet = new Set(second);
        return new List(...first.filter(item => secondSet.has(item)));
    },

    except(first, second, comparer) {
        if (!first) return new List();
        if (!second) return new List(...first);
        
        if (comparer) {
            return new List(...first.filter(item => !second.some(s => comparer(s, item))));
        }
        
        const secondSet = new Set(second);
        return new List(...first.filter(item => !secondSet.has(item)));
    },

    distinct(source, comparer) {
        if (!source) return new List();
        
        if (comparer) {
            const unique = [];
            for (const item of source) {
                if (!unique.some(u => comparer(u, item))) {
                    unique.push(item);
                }
            }
            return new List(...unique);
        }
        
        return new List(...new Set(source));
    },

    // ============================================
    // Partitioning
    // ============================================

    skip(source, count) {
        if (!source) return new List();
        return new List(...Array.from(source).slice(count));
    },

    take(source, count) {
        if (!source) return new List();
        return new List(...Array.from(source).slice(0, count));
    },

    skipWhile(source, predicate) {
        if (!source) return new List();
        const arr = Array.from(source);
        let i = 0;
        while (i < arr.length && predicate(arr[i], i)) i++;
        return new List(...arr.slice(i));
    },

    takeWhile(source, predicate) {
        if (!source) return new List();
        const arr = Array.from(source);
        let i = 0;
        while (i < arr.length && predicate(arr[i], i)) i++;
        return new List(...arr.slice(0, i));
    },

    // ============================================
    // Quantifiers
    // ============================================

    any(source, predicate) {
        if (!source) return false;
        if (predicate) {
            return Array.from(source).some(predicate);
        }
        return Array.from(source).length > 0;
    },

    all(source, predicate) {
        if (!source) return true;
        return Array.from(source).every(predicate);
    },

    contains(source, value, comparer) {
        if (!source) return false;
        if (comparer) {
            return source.some(item => comparer(item, value));
        }
        return Array.from(source).includes(value);
    },

    sequenceEqual(first, second, comparer) {
        if (!first && !second) return true;
        if (!first || !second) return false;
        
        const arr1 = Array.from(first);
        const arr2 = Array.from(second);
        
        if (arr1.length !== arr2.length) return false;
        
        if (comparer) {
            for (let i = 0; i < arr1.length; i++) {
                if (!comparer(arr1[i], arr2[i])) return false;
            }
            return true;
        }
        
        return arr1.every((item, i) => item === arr2[i]);
    },

    // ============================================
    // Conversion
    // ============================================

    toList(source) {
        if (!source) return new List();
        return new List(...source);
    },

    toArray(source) {
        if (!source) return [];
        return Array.from(source);
    },

    toDictionary(source, keySelector, elementSelector) {
        const { Dictionary } = await import('./runtime.js');
        const dict = new Dictionary();
        
        for (const item of (source || [])) {
            const key = keySelector(item);
            const value = elementSelector ? elementSelector(item) : item;
            dict.set(key, value);
        }
        
        return dict;
    },

    toLookup(source, keySelector, elementSelector) {
        return this.groupBy(source, keySelector, elementSelector);
    },

    // ============================================
    // Other
    // ============================================

    defaultIfEmpty(source, defaultValue = null) {
        if (!source || Array.from(source).length === 0) {
            return new List([defaultValue]);
        }
        return new List(...source);
    },

    zip(first, second, resultSelector) {
        if (!first || !second) return new List();
        const arr1 = Array.from(first);
        const arr2 = Array.from(second);
        const length = Math.min(arr1.length, arr2.length);
        const result = [];
        
        for (let i = 0; i < length; i++) {
            result.push(resultSelector(arr1[i], arr2[i], i));
        }
        
        return new List(...result);
    },

    range(start, count) {
        return new List(...Array.from({ length: count }, (_, i) => start + i));
    },

    repeat(element, count) {
        return new List(...Array.from({ length: count }, () => element));
    }
};
