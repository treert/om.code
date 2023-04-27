#include<iostream>
#include<cmath>

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

int main(){
    for (int kk = 0; kk < 256; kk+=2){
        if(!test_num(kk)){
            cout << kk << " : " << test_num(kk) << std::endl;
        }
    }
    // for(int i = 1; i <= 30; i++){
    //     int num = 1<<i;
    //     cout << num << ": ";
    //     for(int k = 0; k < 7; k++){
    //         int pack = (k<<5) + i;
    //         int pp = (((1<<(pack&0b1'1111)) + ((pack>>5)))|1);
    //         for (int kk = 1; kk < 101; kk+=2){
    //             pp = num + kk;
    //             if(helper_IsPrime(pp)){
    //                 cout << pp << " +" << (kk) << ",";
    //                 goto l_nextone;
    //             }
    //         }

    //     }
    //     l_nextone:
    //     cout << std::endl;
    // }
}