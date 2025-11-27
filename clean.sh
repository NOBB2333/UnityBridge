#!/bin/bash

# 清理 .NET 项目的 bin 和 obj 目录

echo "🧹 开始清理项目..."

# 查找并删除所有 bin 目录
echo "删除 bin 目录..."
find . -type d -name "bin" -exec rm -rf {} + 2>/dev/null

# 查找并删除所有 obj 目录
echo "删除 obj 目录..."
find . -type d -name "obj" -exec rm -rf {} + 2>/dev/null

echo "✅ 清理完成！"
