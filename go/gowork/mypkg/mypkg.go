package main

import (
	"flag"
	"fmt"
	"io"
	"math"
	"os"
	"path/filepath"
	"strings"
)

// dotnet System.Random.CompatPrng
type MyRandom struct {
	_seedArray []int32
	_inext     int32
	_inextp    int32
}

func absInt32(n int32) int32 {
	if n < 0 {
		return -n
	}
	return n
}

func (it *MyRandom) Reset(seed int32) {
	array := make([]int32, 56)
	var num, num2, num3, num4 int32
	if seed == math.MinInt32 {
		num = math.MaxInt32
	} else {
		num = absInt32(seed)
	}
	num2 = 161803398 - num
	num3 = 1
	num4 = 0
	array[55] = int32(num2)
	for i := 1; i < 55; i++ {
		num4 += 21
		if num4 >= 55 {
			num4 -= 55
		}
		array[num4] = num3
		num3 = num2 - num3
		if num3 < 0 {
			num3 += math.MaxInt32
		}
		num2 = array[num4]
	}
	for i := 1; i < 5; i++ {
		for k := 1; k < 56; k++ {
			num5 := k + 30
			if num5 >= 55 {
				num5 -= 55
			}
			array[k] -= array[1+num5]
			if array[k] < 0 {
				array[k] += math.MaxInt32
			}
		}
	}
	it._seedArray = array
	it._inext = 0
	it._inextp = 21
}

func (it *MyRandom) Next() int32 {
	inext := it._inext + 1
	if inext >= 56 {
		inext = 1
	}
	inextp := it._inextp + 1
	if inextp >= 56 {
		inextp = 1
	}
	seedArray := it._seedArray
	num := seedArray[inext] - seedArray[inextp]
	if num == math.MaxInt32 {
		num--
	}
	if num < 0 {
		num += math.MaxInt32
	}
	seedArray[inext] = num
	it._inext = inext
	it._inextp = inextp
	return num
}

var args struct {
	unpkg bool
	help  bool
}

func init() {
	flag.BoolVar(&args.unpkg, "u", false, "是否是解码")
	flag.BoolVar(&args.help, "?", false, "帮助")

	flag.Usage = func() {
		fmt.Fprintf(flag.CommandLine.Output(), `Use: mypkg [-u] <paths>
Options:
`)
		flag.PrintDefaults()
	}
}

type MyFiles struct {
	files []string
}

func (it *MyFiles) Add(path string) {
	it.files = append(it.files, path)
}

const _Len = 4096

var _R123Keys = [_Len]byte{}
var _tmp_suffix = ".omtmp"
var _pkg_suffix = ".ompkg"

func _XOR_File(path string) {
	if !strings.HasSuffix(path, _tmp_suffix) {
		panic("only xor omtmp file")
	}
	f, err := os.OpenFile(path, os.O_RDWR, 0666)
	if err != nil {
		panic(err)
	}
	defer f.Close()
	buf := [_Len]byte{}
	var n int
	n, err = f.ReadAt(buf[:], 0)
	if err != nil && err != io.EOF {
		panic(err)
	}
	for i := 0; i < n; i++ {
		buf[i] ^= _R123Keys[i]
	}
	_, err = f.WriteAt(buf[:n], 0)
	if err != nil {
		panic(err)
	}
}

func _MoveFile(src string, dst string) {
	err := os.Rename(src, dst)
	if err != nil {
		panic(err)
	}
}

func HandleOneFile(path string) {
	if strings.HasSuffix(path, _tmp_suffix) {
		fmt.Fprintln(os.Stderr, "[warn] tmp file: ", path)
	} else if strings.HasSuffix(path, _pkg_suffix) {
		if args.unpkg {
			file := path[:len(path)-len(_pkg_suffix)]
			tmpfile := file + _tmp_suffix
			_MoveFile(path, tmpfile)
			_XOR_File(tmpfile)
			_MoveFile(tmpfile, file)
			fmt.Println("unpkg ok:", path)
		}
	} else {
		if !args.unpkg {
			file := path
			tmpfile := file + _tmp_suffix
			pkgfile := file + _pkg_suffix
			_MoveFile(file, tmpfile)
			_XOR_File(tmpfile)
			_MoveFile(tmpfile, pkgfile)
			fmt.Println("pkg ok:", path)
		}
	}
}

func main() {
	flag.Parse()
	if args.help || len(flag.Args()) == 0 {
		flag.Usage()
		return
	}
	myrand := MyRandom{}
	myrand.Reset(123)
	for i := 0; i < len(_R123Keys); i++ {
		b := byte(myrand.Next())
		_R123Keys[i] = b
	}

	// 遍历目录
	myfiles := MyFiles{}
	for _, root := range flag.Args() {
		filepath.Walk(root, func(path string, info os.FileInfo, err error) error {
			if info.IsDir() {
			} else {
				myfiles.Add(path)
			}
			return nil
		})
	}
	for _, path := range myfiles.files {
		HandleOneFile(path)
	}
}
