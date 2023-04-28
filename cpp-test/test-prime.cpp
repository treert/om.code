#include<iostream>
#include<cmath>
#include<cstdio>
#include<vector>

static int helper_IsPrime(int candidate)
{
  if ((candidate & 1) != 0)
  {
    int limit = (int)sqrt(candidate);
    for (int divisor = 3; divisor <= limit; divisor += 2)
    {
      if ((candidate % divisor) == 0)
        return 0;
    }
    return 1;
  }
  return candidate == 2;
}

using std::cout;

bool test_num(int kk){
    for(int i = 1; i <= 30; i++){
        int num = 1<<i;
        bool ok = false;
        for(int k = 0; k < 7; k++){
            int pack = (k<<5) + i;
            int pp = (((1<<(pack&0b1'1111)) + ((pack>>5)))|1);
            pp = pp + kk;
            if(helper_IsPrime(pp)){
                ok = true;
                break;
            }
        }
        if(!ok){
            return false;
        }
    }
    return true;
}

void GenPrime(){
    std::vector<int> primes;
    for(int i = 1; i <= 30; i++){
        int num = 1<<i;
        int k = num*1.23;
        k |= 1;
        for(; k < INT32_MAX; k += 2){
            if((k-1)% 101 != 0 && helper_IsPrime(k)){
                // cout << k << ',';
                primes.push_back(k);
                break;
            }
        }
    }
    // cout << primes.size() << std::endl;
    // for(uint32_t p:primes){
    //     printf("%u,",p);
    // }
    // cout << '\n';
    // for(uint32_t p:primes){
    //     uint64_t m = (UINT64_MAX/p)+1;
    //     printf("%lluull,",m);
    // }

    for(uint32_t p:primes){
        uint64_t m = (UINT64_MAX/p)+1;
        printf("{%u,%lluull},",p, m);
    }

    cout << '\n';
}

int main(){
    GenPrime();
    return 0;
}