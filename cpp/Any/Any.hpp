#pragma once
#ifndef ANY_H
#define ANY_H

#include <algorithm>
#include <typeinfo>
#include <vector>
#include <assert.h>

/**
实现复杂，不建议使用的，可以用`string args_[]`或者`void* args_[]`达到同样的效果。
常用的地方：
1. 事件回调函数的参数

Example1
void function(AnyVec& args_)
{
    assert(args_.size() == 3);
    const Any& param_0 = args_[0];
    auto param_1 = any_cast<std::string>(args_[1]);
    auto param_2 = any_cast<int>(args_[2]);
    // ...
}

AnyVec args;
args.push_back(Any(true));
args.push_back(Any("1"));
args.push_back(Any(2));

function(args);

*/

class Any
{
public: // constructors

    Any() : mContent(0)
    {
    }

    template<typename ValueType>
    explicit Any(const ValueType & value)
        : mContent(new holder<ValueType>(value))
    {
    }

    Any(const Any & other)
        : mContent(other.mContent ? other.mContent->clone() : 0)
    {
    }

    virtual ~Any()
    {
        destroy();
    }

public: // modifiers

    Any& swap(Any & rhs)
    {
        std::swap(mContent, rhs.mContent);
        return *this;
    }

    template<typename ValueType>
    Any& operator=(const ValueType & rhs)
    {
        Any(rhs).swap(*this);
        return *this;
    }

    Any & operator=(const Any & rhs)
    {
        Any(rhs).swap(*this);
        return *this;
    }

public: // queries

    bool isEmpty() const
    {
        return !mContent;
    }

    const std::type_info& getType() const
    {
        return mContent ? mContent->getType() : typeid(void);
    }

    inline friend std::ostream& operator <<
    ( std::ostream& o, const Any& v )
    {
        if (v.mContent)
            v.mContent->writeToStream(o);
        return o;
    }

    void destroy()
    {
        if(mContent)
        {
            delete mContent;
            mContent = NULL;
        }
    }

protected: // types

    class placeholder
    {
    public: // structors

        virtual ~placeholder()
        {
        }

    public: // queries

        virtual const std::type_info& getType() const = 0;

        virtual placeholder * clone() const = 0;

        virtual void writeToStream(std::ostream& o) = 0;

    };

    template<typename ValueType>
    class holder : public placeholder
    {
    public: // structors

        holder(const ValueType & value)
            : held(value)
        {
        }

    public: // queries

        virtual const std::type_info & getType() const
        {
            return typeid(ValueType);
        }

        virtual placeholder * clone() const
        {
            return new holder(held);
        }

        virtual void writeToStream(std::ostream& o)
        {
            o << held;
        }


    public: // representation
        ValueType held;
    };

protected: // representation
    placeholder * mContent;
    template<typename ValueType>
    friend ValueType * any_cast(Any *);

public:
    template<typename ValueType>
    ValueType operator()() const
    {
        assert(mContent && "mContent can not be nullptr");
        assert((getType() == typeid(ValueType)) && "type error");
        return static_cast<Any::holder<ValueType> *>(mContent)->held;
    }

    template <typename ValueType>
    ValueType get(void) const
    {
        assert(mContent && "mContent can not be nullptr");
        assert((getType() == typeid(ValueType)) && "type error");
        return static_cast<Any::holder<ValueType> *>(mContent)->held;
    }
};


/** Specialised Any class which has built in arithmetic operators, but can
                hold only types which support operator +,-,* and / .
        */
class AnyNumeric : public Any
{
public:
    AnyNumeric()
        : Any()
    {
    }

    template<typename ValueType>
    AnyNumeric(const ValueType & value)

    {
        mContent = new numholder<ValueType>(value);
    }

    AnyNumeric(const AnyNumeric & other)
        : Any()
    {
        mContent = other.mContent ? other.mContent->clone() : 0;
    }

protected:
    class numplaceholder : public Any::placeholder
    {
    public: // structors

        ~numplaceholder()
        {
        }
        virtual placeholder* add(placeholder* rhs) = 0;
        virtual placeholder* subtract(placeholder* rhs) = 0;
        virtual placeholder* multiply(placeholder* rhs) = 0;
        virtual placeholder* multiply(float factor) = 0;
        virtual placeholder* divide(placeholder* rhs) = 0;
    };

    template<typename ValueType>
    class numholder : public numplaceholder
    {
    public: // structors

        numholder(const ValueType & value)
            : held(value)
        {
        }

    public: // queries

        virtual const std::type_info & getType() const
        {
            return typeid(ValueType);
        }

        virtual placeholder * clone() const
        {
            return new numholder(held);
        }

        virtual placeholder* add(placeholder* rhs)
        {
            return new numholder(held + static_cast<numholder*>(rhs)->held);
        }
        virtual placeholder* subtract(placeholder* rhs)
        {
            return new numholder(held - static_cast<numholder*>(rhs)->held);
        }
        virtual placeholder* multiply(placeholder* rhs)
        {
            return new numholder(held * static_cast<numholder*>(rhs)->held);
        }
        virtual placeholder* multiply(float factor)
        {
            return new numholder(held * factor);
        }
        virtual placeholder* divide(placeholder* rhs)
        {
            return new numholder(held / static_cast<numholder*>(rhs)->held);
        }
        virtual void writeToStream(std::ostream& o)
        {
            o << held;
        }

    public: // representation
        ValueType held;
    };

    /// Construct from holder
    AnyNumeric(placeholder* pholder)
    {
        mContent = pholder;
    }

public:
    AnyNumeric & operator=(const AnyNumeric & rhs)
    {
        AnyNumeric(rhs).swap(*this);
        return *this;
    }
    AnyNumeric operator+(const AnyNumeric& rhs) const
    {
        return AnyNumeric(
                    static_cast<numplaceholder*>(mContent)->add(rhs.mContent));
    }
    AnyNumeric operator-(const AnyNumeric& rhs) const
    {
        return AnyNumeric(
                    static_cast<numplaceholder*>(mContent)->subtract(rhs.mContent));
    }
    AnyNumeric operator*(const AnyNumeric& rhs) const
    {
        return AnyNumeric(
                    static_cast<numplaceholder*>(mContent)->multiply(rhs.mContent));
    }
    AnyNumeric operator*(float factor) const
    {
        return AnyNumeric(
                    static_cast<numplaceholder*>(mContent)->multiply(factor));
    }
    AnyNumeric operator/(const AnyNumeric& rhs) const
    {
        return AnyNumeric(
                    static_cast<numplaceholder*>(mContent)->divide(rhs.mContent));
    }
    AnyNumeric& operator+=(const AnyNumeric& rhs)
    {
        *this = AnyNumeric(
                    static_cast<numplaceholder*>(mContent)->add(rhs.mContent));
        return *this;
    }
    AnyNumeric& operator-=(const AnyNumeric& rhs)
    {
        *this = AnyNumeric(
                    static_cast<numplaceholder*>(mContent)->subtract(rhs.mContent));
        return *this;
    }
    AnyNumeric& operator*=(const AnyNumeric& rhs)
    {
        *this = AnyNumeric(
                    static_cast<numplaceholder*>(mContent)->multiply(rhs.mContent));
        return *this;
    }
    AnyNumeric& operator/=(const AnyNumeric& rhs)
    {
        *this = AnyNumeric(
                    static_cast<numplaceholder*>(mContent)->divide(rhs.mContent));
        return *this;
    }
};


template<typename ValueType>
ValueType * any_cast(Any * operand)
{
    return operand && (strcmp(operand->getType().name(), typeid(ValueType).name()) == 0)
            ? &static_cast<Any::holder<ValueType> *>(operand->mContent)->held
            : 0;
}

template<typename ValueType>
const ValueType * any_cast(const Any * operand)
{
    return any_cast<ValueType>(const_cast<Any *>(operand));
}

template<typename ValueType>
ValueType any_cast(const Any & operand)
{
    const ValueType * result = any_cast<ValueType>(&operand);
    if(!result)
    {
        /*
        StringUtil::StrStreamType str;
        str << "Bad cast from type '" << operand.getType().name() << "' "
            << "to '" << typeid(ValueType).name() << "'";
        OGRE_EXCEPT(Exception::ERR_INVALIDPARAMS,
                    str.str(),
                    "Ogre::any_cast");*/
    }
    return *result;
}

typedef std::vector<Any> AnyVec;

#endif // ANY_H
