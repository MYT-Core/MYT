// Copyright (c) 2014-2024, The Monero Project
//
// ==================================================================================
// MYT - A privacy-focused Proof-of-Work cryptocurrency
// Copyright (c) 2026, MYT
//
// This file has been modified for the MYT project.
// Original Monero code preserved above as required by the BSD-3 license.
// ==================================================================================
//
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without modification, are
// permitted provided that the following conditions are met:
//
// 1. Redistributions of source code must retain the above copyright notice, this list of
//    conditions and the following disclaimer.
//
// 2. Redistributions in binary form must reproduce the above copyright notice, this list
//    of conditions and the following disclaimer in the documentation and/or other
//    materials provided with the distribution.
//
// 3. Neither the name of the copyright holder nor the names of its contributors may be
//    used to endorse or promote products derived from this software without specific
//    prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY
// EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF
// MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL
// THE COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
// PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
// INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
// STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF
// THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//
// Parts of this file are originally copyright (c) 2012-2013 The Cryptonote developers

#include "gtest/gtest.h"

#include <algorithm>
#include <array>
#include <limits>

#include "checkpoints/checkpoints.cpp"
#include "cryptonote_basic/cryptonote_format_utils.h"
#include "cryptonote_core/cryptonote_tx_utils.h"

using namespace cryptonote;

namespace
{
  constexpr const char *MAINNET_GENESIS_HASH = "faa1d2461b7290cbcf957201ea735957cf17605266b3d3cbda4da79691642b9a";
  constexpr const char *TESTNET_STAGENET_GENESIS_HASH = "595fc17496e37bd3a2e9895686731b4fb1e19a00e2b682b4181b1da2543f4d5f";

  const std::array<const char *, 7> FOREIGN_TESTNET_STAGENET_HASHES = {{
    "48ca7cd3c8de5b6a4d53d2861fbdaedca141553559f9be9520068053cda8430b",
    "46b690b710a07ea051bc4a6b6842ac37be691089c0f7758cfeec4d5fc0b4a258",
    "12904f6b4d9e60fd875674e07147d2c83d6716253f046af7b894c3e81da7e1bd",
    "87562ca6786f41556b8d5b48067303a57dc5ca77155b35199aedaeca1550f5a0",
    "76ee3cc98646292206cd3e86f74d88b4dcc1d937088645e9b0cbca84b7ce74eb",
    "1f8b0ce313f8b9ba9a46108bfd285c45ad7c2176871fd41c3a690d4830ce2fd5",
    "409f68cddd8e74b37469b41c1e61250d81c5776b42264f416d5d27c4626383ed"
  }};

  crypto::hash parse_hash(const char *hash_string)
  {
    crypto::hash hash{};
    EXPECT_TRUE(epee::string_tools::hex_to_pod(hash_string, hash));
    return hash;
  }

  checkpoints default_checkpoints(network_type nettype)
  {
    checkpoints points;
    EXPECT_TRUE(points.init_default_checkpoints(nettype));
    return points;
  }

  void expect_genesis_only(network_type nettype, const char *expected_hash)
  {
    const checkpoints points = default_checkpoints(nettype);
    const auto &hashes = points.get_points();
    const auto &difficulties = points.get_difficulty_points();

    ASSERT_EQ(1, hashes.size());
    ASSERT_EQ(1, difficulties.size());
    EXPECT_EQ(0, hashes.begin()->first);
    EXPECT_EQ(parse_hash(expected_hash), hashes.begin()->second);
    EXPECT_EQ(0, difficulties.begin()->first);
    EXPECT_EQ(difficulty_type{1}, difficulties.begin()->second);
  }

  crypto::hash generated_genesis_hash(network_type nettype)
  {
    const config_t &config = get_config(nettype);
    block genesis;
    EXPECT_TRUE(generate_genesis_block(genesis, config.GENESIS_TX, config.GENESIS_NONCE, config.GENESIS_TIMESTAMP));
    return get_block_hash(genesis);
  }
}

TEST(checkpoints_default_tables, mainnet_genesis_is_unchanged)
{
  expect_genesis_only(MAINNET, MAINNET_GENESIS_HASH);
  EXPECT_EQ(parse_hash(MAINNET_GENESIS_HASH), generated_genesis_hash(MAINNET));
}

TEST(checkpoints_default_tables, testnet_contains_only_myt_genesis)
{
  expect_genesis_only(TESTNET, TESTNET_STAGENET_GENESIS_HASH);
  EXPECT_EQ(parse_hash(TESTNET_STAGENET_GENESIS_HASH), generated_genesis_hash(TESTNET));
}

TEST(checkpoints_default_tables, stagenet_contains_only_myt_genesis)
{
  expect_genesis_only(STAGENET, TESTNET_STAGENET_GENESIS_HASH);
  EXPECT_EQ(parse_hash(TESTNET_STAGENET_GENESIS_HASH), generated_genesis_hash(STAGENET));
}

TEST(checkpoints_default_tables, reachable_testnet_and_stagenet_tables_have_no_foreign_hashes)
{
  for (const network_type nettype : {TESTNET, STAGENET})
  {
    const checkpoints default_points = default_checkpoints(nettype);
    const auto &points = default_points.get_points();
    for (const char *foreign_hash : FOREIGN_TESTNET_STAGENET_HASHES)
    {
      const crypto::hash parsed_foreign_hash = parse_hash(foreign_hash);
      EXPECT_EQ(points.end(), std::find_if(points.begin(), points.end(), [&parsed_foreign_hash](const auto &point) {
        return point.second == parsed_foreign_hash;
      }));
    }
  }
}

TEST(checkpoints_default_tables, configured_genesis_hash_is_accepted)
{
  for (const network_type nettype : {MAINNET, TESTNET, STAGENET})
  {
    const checkpoints points = default_checkpoints(nettype);
    bool is_checkpoint = false;
    EXPECT_TRUE(points.check_block(0, generated_genesis_hash(nettype), is_checkpoint));
    EXPECT_TRUE(is_checkpoint);
  }
}

TEST(checkpoints_default_tables, mutated_genesis_hash_is_rejected)
{
  for (const network_type nettype : {MAINNET, TESTNET, STAGENET})
  {
    const checkpoints points = default_checkpoints(nettype);
    crypto::hash mutated_hash = generated_genesis_hash(nettype);
    mutated_hash.data[0] ^= 0x01;
    bool is_checkpoint = false;
    EXPECT_FALSE(points.check_block(0, mutated_hash, is_checkpoint));
    EXPECT_TRUE(is_checkpoint);
  }
}

TEST(checkpoints_default_tables, genesis_only_checkpoint_zone_is_exact)
{
  for (const network_type nettype : {MAINNET, TESTNET, STAGENET})
  {
    const checkpoints points = default_checkpoints(nettype);
    EXPECT_TRUE(points.is_in_checkpoint_zone(0));
    EXPECT_FALSE(points.is_in_checkpoint_zone(1));
    EXPECT_FALSE(points.is_in_checkpoint_zone(std::numeric_limits<uint64_t>::max()));
  }
}

TEST(checkpoints_default_tables, removed_heights_cannot_be_wallet_fast_refresh_anchors)
{
  const checkpoints testnet_points = default_checkpoints(TESTNET);
  const checkpoints stagenet_points = default_checkpoints(STAGENET);

  for (const uint64_t stop_height : {1000001ULL, 1058601ULL, 1450001ULL})
  {
    EXPECT_EQ(0, testnet_points.get_nearest_checkpoint_height(stop_height));
    EXPECT_EQ(parse_hash(TESTNET_STAGENET_GENESIS_HASH), testnet_points.get_points().at(0));
  }
  for (const uint64_t stop_height : {10001ULL, 550001ULL})
  {
    EXPECT_EQ(0, stagenet_points.get_nearest_checkpoint_height(stop_height));
    EXPECT_EQ(parse_hash(TESTNET_STAGENET_GENESIS_HASH), stagenet_points.get_points().at(0));
  }
}


TEST(checkpoints_is_alternative_block_allowed, handles_empty_checkpoints)
{
  checkpoints cp;

  ASSERT_FALSE(cp.is_alternative_block_allowed(0, 0));

  ASSERT_TRUE(cp.is_alternative_block_allowed(1, 1));
  ASSERT_TRUE(cp.is_alternative_block_allowed(1, 9));
  ASSERT_TRUE(cp.is_alternative_block_allowed(9, 1));
}

TEST(checkpoints_is_alternative_block_allowed, handles_one_checkpoint)
{
  checkpoints cp;
  ASSERT_TRUE(cp.add_checkpoint(5, "0000000000000000000000000000000000000000000000000000000000000000"));

  ASSERT_FALSE(cp.is_alternative_block_allowed(0, 0));

  ASSERT_TRUE (cp.is_alternative_block_allowed(1, 1));
  ASSERT_TRUE (cp.is_alternative_block_allowed(1, 4));
  ASSERT_TRUE (cp.is_alternative_block_allowed(1, 5));
  ASSERT_TRUE (cp.is_alternative_block_allowed(1, 6));
  ASSERT_TRUE (cp.is_alternative_block_allowed(1, 9));

  ASSERT_TRUE (cp.is_alternative_block_allowed(4, 1));
  ASSERT_TRUE (cp.is_alternative_block_allowed(4, 4));
  ASSERT_TRUE (cp.is_alternative_block_allowed(4, 5));
  ASSERT_TRUE (cp.is_alternative_block_allowed(4, 6));
  ASSERT_TRUE (cp.is_alternative_block_allowed(4, 9));

  ASSERT_FALSE(cp.is_alternative_block_allowed(5, 1));
  ASSERT_FALSE(cp.is_alternative_block_allowed(5, 4));
  ASSERT_FALSE(cp.is_alternative_block_allowed(5, 5));
  ASSERT_TRUE (cp.is_alternative_block_allowed(5, 6));
  ASSERT_TRUE (cp.is_alternative_block_allowed(5, 9));

  ASSERT_FALSE(cp.is_alternative_block_allowed(6, 1));
  ASSERT_FALSE(cp.is_alternative_block_allowed(6, 4));
  ASSERT_FALSE(cp.is_alternative_block_allowed(6, 5));
  ASSERT_TRUE (cp.is_alternative_block_allowed(6, 6));
  ASSERT_TRUE (cp.is_alternative_block_allowed(6, 9));

  ASSERT_FALSE(cp.is_alternative_block_allowed(9, 1));
  ASSERT_FALSE(cp.is_alternative_block_allowed(9, 4));
  ASSERT_FALSE(cp.is_alternative_block_allowed(9, 5));
  ASSERT_TRUE (cp.is_alternative_block_allowed(9, 6));
  ASSERT_TRUE (cp.is_alternative_block_allowed(9, 9));
}

TEST(checkpoints_is_alternative_block_allowed, handles_two_and_more_checkpoints)
{
  checkpoints cp;
  ASSERT_TRUE(cp.add_checkpoint(5, "0000000000000000000000000000000000000000000000000000000000000000"));
  ASSERT_TRUE(cp.add_checkpoint(9, "0000000000000000000000000000000000000000000000000000000000000000"));

  ASSERT_FALSE(cp.is_alternative_block_allowed(0, 0));

  ASSERT_TRUE (cp.is_alternative_block_allowed(1, 1));
  ASSERT_TRUE (cp.is_alternative_block_allowed(1, 4));
  ASSERT_TRUE (cp.is_alternative_block_allowed(1, 5));
  ASSERT_TRUE (cp.is_alternative_block_allowed(1, 6));
  ASSERT_TRUE (cp.is_alternative_block_allowed(1, 8));
  ASSERT_TRUE (cp.is_alternative_block_allowed(1, 9));
  ASSERT_TRUE (cp.is_alternative_block_allowed(1, 10));
  ASSERT_TRUE (cp.is_alternative_block_allowed(1, 11));

  ASSERT_TRUE (cp.is_alternative_block_allowed(4, 1));
  ASSERT_TRUE (cp.is_alternative_block_allowed(4, 4));
  ASSERT_TRUE (cp.is_alternative_block_allowed(4, 5));
  ASSERT_TRUE (cp.is_alternative_block_allowed(4, 6));
  ASSERT_TRUE (cp.is_alternative_block_allowed(4, 8));
  ASSERT_TRUE (cp.is_alternative_block_allowed(4, 9));
  ASSERT_TRUE (cp.is_alternative_block_allowed(4, 10));
  ASSERT_TRUE (cp.is_alternative_block_allowed(4, 11));

  ASSERT_FALSE(cp.is_alternative_block_allowed(5, 1));
  ASSERT_FALSE(cp.is_alternative_block_allowed(5, 4));
  ASSERT_FALSE(cp.is_alternative_block_allowed(5, 5));
  ASSERT_TRUE (cp.is_alternative_block_allowed(5, 6));
  ASSERT_TRUE (cp.is_alternative_block_allowed(5, 8));
  ASSERT_TRUE (cp.is_alternative_block_allowed(5, 9));
  ASSERT_TRUE (cp.is_alternative_block_allowed(5, 10));
  ASSERT_TRUE (cp.is_alternative_block_allowed(5, 11));

  ASSERT_FALSE(cp.is_alternative_block_allowed(6, 1));
  ASSERT_FALSE(cp.is_alternative_block_allowed(6, 4));
  ASSERT_FALSE(cp.is_alternative_block_allowed(6, 5));
  ASSERT_TRUE (cp.is_alternative_block_allowed(6, 6));
  ASSERT_TRUE (cp.is_alternative_block_allowed(6, 8));
  ASSERT_TRUE (cp.is_alternative_block_allowed(6, 9));
  ASSERT_TRUE (cp.is_alternative_block_allowed(6, 10));
  ASSERT_TRUE (cp.is_alternative_block_allowed(6, 11));

  ASSERT_FALSE(cp.is_alternative_block_allowed(8, 1));
  ASSERT_FALSE(cp.is_alternative_block_allowed(8, 4));
  ASSERT_FALSE(cp.is_alternative_block_allowed(8, 5));
  ASSERT_TRUE (cp.is_alternative_block_allowed(8, 6));
  ASSERT_TRUE (cp.is_alternative_block_allowed(8, 8));
  ASSERT_TRUE (cp.is_alternative_block_allowed(8, 9));
  ASSERT_TRUE (cp.is_alternative_block_allowed(8, 10));
  ASSERT_TRUE (cp.is_alternative_block_allowed(8, 11));

  ASSERT_FALSE(cp.is_alternative_block_allowed(9, 1));
  ASSERT_FALSE(cp.is_alternative_block_allowed(9, 4));
  ASSERT_FALSE(cp.is_alternative_block_allowed(9, 5));
  ASSERT_FALSE(cp.is_alternative_block_allowed(9, 6));
  ASSERT_FALSE(cp.is_alternative_block_allowed(9, 8));
  ASSERT_FALSE(cp.is_alternative_block_allowed(9, 9));
  ASSERT_TRUE (cp.is_alternative_block_allowed(9, 10));
  ASSERT_TRUE (cp.is_alternative_block_allowed(9, 11));

  ASSERT_FALSE(cp.is_alternative_block_allowed(10, 1));
  ASSERT_FALSE(cp.is_alternative_block_allowed(10, 4));
  ASSERT_FALSE(cp.is_alternative_block_allowed(10, 5));
  ASSERT_FALSE(cp.is_alternative_block_allowed(10, 6));
  ASSERT_FALSE(cp.is_alternative_block_allowed(10, 8));
  ASSERT_FALSE(cp.is_alternative_block_allowed(10, 9));
  ASSERT_TRUE (cp.is_alternative_block_allowed(10, 10));
  ASSERT_TRUE (cp.is_alternative_block_allowed(10, 11));

  ASSERT_FALSE(cp.is_alternative_block_allowed(11, 1));
  ASSERT_FALSE(cp.is_alternative_block_allowed(11, 4));
  ASSERT_FALSE(cp.is_alternative_block_allowed(11, 5));
  ASSERT_FALSE(cp.is_alternative_block_allowed(11, 6));
  ASSERT_FALSE(cp.is_alternative_block_allowed(11, 8));
  ASSERT_FALSE(cp.is_alternative_block_allowed(11, 9));
  ASSERT_TRUE (cp.is_alternative_block_allowed(11, 10));
  ASSERT_TRUE (cp.is_alternative_block_allowed(11, 11));
}
