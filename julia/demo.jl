
# ============================================================
# Julia 核心特性演示：多重派发、类型系统、元编程、性能
# 场景：模拟一个简单的生态竞争模型 (Lotka-Volterra + 扩展)
# ============================================================

# ---------- 1. 自定义类型与参数化 ----------
abstract type Species end

struct Rabbit <: Species
    name::String
    birth_rate::Float64
    death_rate::Float64
end

struct Fox <: Species
    name::String
    predation_rate::Float64
    food_efficiency::Float64
    death_rate::Float64
end

# ---------- 2. 多重派发 (Multiple Dispatch) ----------
"""每种物种的种群增长率公式不同"""
function growth_rate(s::Rabbit, prey::Float64, predator::Float64)
    return s.birth_rate * prey - s.death_rate * prey * predator
end

function growth_rate(s::Fox, prey::Float64, predator::Float64)
    return s.food_efficiency * prey * predator - s.death_rate * predator
end

# ---------- 3. 泛型函数 + 性能优化 ----------
function simulate(
    rabbit::Rabbit,
    fox::Fox,
    initial_rabbits::Float64,
    initial_foxes::Float64,
    timesteps::Int,
    dt::Float64 = 0.01
)
    # 预分配数组（性能关键：避免动态扩展）
    rabbits = Vector{Float64}(undef, timesteps)
    foxes   = Vector{Float64}(undef, timesteps)
    rabbits[1] = initial_rabbits
    foxes[1]   = initial_foxes

    for t in 1:timesteps-1
        r = rabbits[t]
        f = foxes[t]
        # 钳制到 >= 0，防止欧拉法溢出
        rabbits[t+1] = max(r + dt * growth_rate(rabbit, r, f), 0.0)
        foxes[t+1]   = max(f + dt * growth_rate(fox, r, f), 0.0)
    end

    return rabbits, foxes
end

# ---------- 4. 元编程 @generated 函数 ----------
"""编译期生成针对具体类型的标签文字（演示 @generated）"""
@generated function species_label(::Type{T}) where {T <: Species}
    quote
        "$(string(T)) ★"
    end
end

# ---------- 5. 广播和向量化 ----------
function normalized(population::Vector{Float64})
    max_val = maximum(population)
    return population ./ max_val  # 逐元素除法
end

# ---------- 6. 宏定义 ----------
macro time_it(expr)
    quote
        start = time()
        result = $(esc(expr))
        elapsed = time() - start
        println("⏱ 耗时: $(round(elapsed, digits=6)) 秒")
        result
    end
end

# ======================== 主程序 ========================
function main()
    println("============================================")
    println("  Julia 样例：生态竞争模型")
    println("============================================")

    # 初始化
    rabbit = Rabbit("白兔", 1.0, 0.1)
    fox    = Fox("赤狐", 0.05, 0.01, 0.5)

    println("\n物种标签（编译期生成）:")
    println("  ", species_label(Rabbit), "  &  ", species_label(Fox))

    println("\n初始条件:")
    println("  兔子: 40.0,  狐狸: 9.0")
    println("  时间步: 2000 步\n")

    # 运行模拟（计时）
    rabbits, foxes = @time_it simulate(rabbit, fox, 40.0, 9.0, 2000)

    # 结果统计
    norm_r = normalized(rabbits)
    norm_f = normalized(foxes)

    println("结果统计:")
    println("  兔子最终数量: $(round(rabbits[end], digits=2))")
    println("  狐狸最终数量: $(round(foxes[end], digits=2))")
    println("  兔子数量范围: [$(round(minimum(rabbits), digits=2)), $(round(maximum(rabbits), digits=2))]")
    println("  狐狸数量范围: [$(round(minimum(foxes), digits=2)), $(round(maximum(foxes), digits=2))]")

    # 振荡周期分析
    rabbit_crossings = 0
    for i in 2:length(norm_r)-1
        if (norm_r[i] > norm_f[i]) != (norm_r[i+1] > norm_f[i+1])
            rabbit_crossings += 1
        end
    end
    println("  种群曲线交叉次数: $rabbit_crossings")

    println("\nJulia 版本: ", VERSION)
    println("============================================")
end

main()
