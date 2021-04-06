﻿//------------------------------------------------------------------------------
//
// Copyright (c) Microsoft Corporation.
// All rights reserved.
//
// This code is licensed under the MIT License.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files(the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions :
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Json;
using Microsoft.IdentityModel.Json.Linq;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Microsoft.IdentityModel.TestExtensions
{
    /// <summary>
    /// A class responsible for creating test tokens for use in unit testing implementations depending on
    /// Microsoft.IdentityModel token validation.
    /// </summary>
    /// <remarks>
    /// Microsoft.IdentityModel.SampleTests.SampleTokenValidationClassTests contains examples for how this class can be leveraged
    /// to validate a trivial token validation class that depends on Microsoft.IdentityModel's token validation methods.
    /// </remarks>
    /// <example>
    /// The following provides an example for how this class could be leveraged using a common testing framework, Xunit. The core concepts
    /// will be applicable to unit testing using any framework however.
    ///
    /// The example imagines a class, ClassWithMicrosoftIdentityModelDependency, which exposes ValidateToken, a method calling the
    /// Microsoft.IdentityModel library and GetTokenValidationParameters which retrieves the <see cref="TokenValidationParameters"/>
    /// the code under test actually uses. Note that it's important to use the real <see cref="TokenValidationParameters"/> since that
    /// will allow the unit tests to actually confirm if there's a hole in the validation (e.g. certain important validation is disabled,
    /// <see cref="TokenValidationParameters.ValidateAudience"/>, <see cref="TokenValidationParameters.ValidateIssuer"/>, etc.)
    ///
    /// In the following code example, generateTokenToTest should be one of the methods from this class.
    /// 
    /// <code>
    /// internal void AssertValidationException(Func<string> generateTokenToTest, Type innerExceptionType, string innerExceptionMessagePart)
    /// {
    ///     try
    ///     {
    ///         ClassWithMicrosoftIdentityModelDependency.ValidateToken(
    ///             generateTokenToTest,
    ///             ClassWithMicrosoftIdentityModelDependency.GetTokenValidationParameters());
    /// 
    ///         if (innerExceptionType != null || innerExceptionType != null)
    ///             throw new TestException(
    ///                 string.Format(
    ///                     "Expected an exception of type '{0}' containing '{1}' in the message.",
    ///                     innerExceptionType,
    ///                     innerExceptionMessagePart));
    ///     }
    ///     catch (Exception e)
    ///     {
    ///         Assert.Equal(typeof(SampleTestTokenValidationException), e.GetType());
    ///         Assert.Equal(innerExceptionType, e.InnerException.GetType());
    /// 
    ///         if (!string.IsNullOrEmpty(innerExceptionMessagePart))
    ///         {
    ///             Assert.Contains(innerExceptionMessagePart, e.InnerException.Message);
    ///         }
    ///     }
    /// }
    ///
    /// [Fact]
    /// public void TokenWithoutSignature()
    /// {
    ///     var testTokenCreator = new TestTokenCreator();
    ///     AssertValidationException(
    ///         testTokenCreator.CreateTokenWithNoSignature,
    ///         typeof(ArgumentException),
    ///         "IDX14111");
    /// }
    /// 
    /// </code>
    /// </example>
    public class TestTokenCreator
    {
        /// <summary>
        /// The default test issuer.
        /// </summary>
        private const string _defaultTestIssuer = "http://Default.Issuer.com";

        /// <summary>
        /// The default test audience.
        /// </summary>
        private const string _defaultTestAudience = "http://Default.Audience.com";

        /// <summary>
        /// The default claim value for IssuedAt and NotBefore.
        /// </summary>
        private static DateTime _defaultIssuedAtNotBefore = DateTime.UtcNow - TimeSpan.FromHours(1);

        /// <summary>
        /// The default claim value for Expiration Time.
        /// </summary>
        private static DateTime _defaultExpirationTime = DateTime.UtcNow + TimeSpan.FromHours(23);

        /// <summary>
        /// The default set of claims for test tokens generated by this class.
        /// </summary>
        private static List<Claim> _payloadClaims = new List<Claim>()
        {
            new Claim(JwtRegisteredClaimNames.Email, "Bob@contoso.com", ClaimValueTypes.String, _defaultTestIssuer, _defaultTestIssuer),
            new Claim(JwtRegisteredClaimNames.GivenName, "Bob", ClaimValueTypes.String, _defaultTestIssuer, _defaultTestIssuer),
            new Claim(JwtRegisteredClaimNames.Iss, _defaultTestIssuer, ClaimValueTypes.String, _defaultTestIssuer, _defaultTestIssuer),
            new Claim(JwtRegisteredClaimNames.Aud, _defaultTestAudience, ClaimValueTypes.String, _defaultTestIssuer, _defaultTestIssuer),
            new Claim(JwtRegisteredClaimNames.Iat, EpochTime.GetIntDate(_defaultIssuedAtNotBefore).ToString(), ClaimValueTypes.String, _defaultTestIssuer, _defaultTestIssuer),
            new Claim(JwtRegisteredClaimNames.Nbf, EpochTime.GetIntDate(_defaultIssuedAtNotBefore).ToString(), ClaimValueTypes.String, _defaultTestIssuer, _defaultTestIssuer),
            new Claim(JwtRegisteredClaimNames.Exp, EpochTime.GetIntDate(_defaultExpirationTime).ToString(), ClaimValueTypes.String, _defaultTestIssuer, _defaultTestIssuer),
        };

        /// <summary>
        /// A set of <see cref="RSAParameters"/> which should be different than what a caller provides as their SigningCredentials.
        /// </summary>
        private static RSAParameters _rsaParameters_4096 = new RSAParameters
        {
            D = Base64UrlEncoder.DecodeBytes("B15EJtB2qDEtE2z4k6dR-o_fQGoFtQR10FfMWt3XSE44jPYLMKyr3cc0NQkw7DsMpU8Olqo-enIX1BD9vVdWXAY-gR4yxJtRyiVcdAJiNHH6GN8s_GA21Z-6AZR_FTGgmJStEUe-KupSwzYdjo6f5S8dHyaJpLuRhhIx8mLExdhA7543Puwt-MMKj46rcEEKXhe2jNZl1vDgj8-71ZjTvE6HW__invbNX3ZefrZJCD-rDI9SyCkktq9msKfodFtyKfQ0-EYG6rxk7hpAwtilr418N-0I7oYh4OJHC44sRQswL7aCuxHB6qJbkPm7D5QIrrdd4ts5hYNoHd3YJIhr_H09VFnql5CfIyk2NbxHwbZAkGuA00ceBJ0RuIWvrxf-66ZbcNUz2U7l2GEF5qUsy7T4Zi6JtH7I3b4EaO620X-SZ5GXBWmxWBthVi2St3z7vAGl6Nv6F-pk7IjVZ4jj3EJGgCpHZEZObAD7y5IpslBiA4F24qitYfI4RPuncz78V2aKu8Zi6O19PFqQs1Ky3fEV5-fQjPqtu9944omHiwx0Jsjy0SU5MdM6indY75sTKM0xmOFCrF4poGFGJMHW-PJmFy2pCC9O39iz9ki32vJlvYo085a5vrVzKni_fY3HOJKC0NMMlwvAasedT2ZeN1hVVjSqlQwjGQTmDjcx07k"),
            DP = Base64UrlEncoder.DecodeBytes("obx_Oz4PLLgDWeIU5JnQLFecm-Ic7DycYevfXtinoMwCTqEO04FBL90XHI5tEZRTRMm_1_uuYLA3tAxlLCSBLt922KP0SLkxhkn2WnrxFzBng6ZgNPtq5tss8svKaWiqAaPR0V8UAtWqEaLVktzreeQRdL_2aWyZHYAQRXdjHyNXKNt2yMcT3hYAD0Hwwue6gqk8G3t-E-pYdbQKJPpGG-wBepnsXBkdBy2Lw7wb1gECrEScljP-Fh8eCr7vOIpyhFcLrTUeVU0mDc0-lcRTk-JPaHhAov3tLuCkOv05r4yAllRLzlEZGnZksuKp47vgKtdkB8heChCxAyC9agESGQ"),
            DQ = Base64UrlEncoder.DecodeBytes("cd6SJC-Qp996mJYB2OJHiPcxPKxKgyAzHE-4A0oiJM_3wjjAjZ92TLY6fDbTtIL3xsoEbxL_gR1sEWlTzk97-mW9ErxsIHFI9UJxwnlGm2ljOL7-Juywd4U5BPhXYBqlZe0EDGxuREOnC6wMKp9-6zUxrN-sS2GK6n9Sieg24FDoQMX3U5CTDaxnQGUUtFYFDVZqFsZNmIf4N-vYqHcp2LwNZO9XC8sotJ0tXO40PUteHCmmFsglmFmNJeHYf0dRM3pwyu-4s1-7xWbPi2cZUKYVMLYGc6RJ_-VYJRrxAhTPX54Ow2hupjQIK0AUMrq3dgQpqqaGBl2L8gBAkzYHaQ"),
            Exponent = Base64UrlEncoder.DecodeBytes("AQAB"),
            InverseQ = Base64UrlEncoder.DecodeBytes("GwF0PXhYOz8Fwhz2JyxoZ_a_L7IwhbEhfPpYGXj6XBBkHztQtXvEb5dlNlK2Y6vQnfFgA1l5jiMZiFqnTSq6z1amWkrQUutqWq-EGo8jVVxzxx5as2t7lAohAn1dSY-HnP6ig0e8JF0rKeB8bxYqe-tPbVpuOgDbFKcOoaMCLRJY7kTRaGROss0O7bIJYu_g7lE4UPfybr2wEVy_3VucR279PZiwj3z-X6hMeUQHn-js2l1RGc-QgeEu2O7y2hq-APWnvna19xK8t8R5AS6hUnYeYTcZb4McpI7cg6H_5gItSFYuErPEGZWEZst8mcB2B6Yg9-WR7epmwZbSMCVt9A"),
            Modulus = Base64UrlEncoder.DecodeBytes("vfPmqjbDDsT_xnh5CfnR97GirbRASP3rKyUlLOpytrkkWtNSD2S0ovnFlW5H4E9OiGusUpeJYD6HmAY5VSafWKC0qVEAvFIT_Fs6j8kJ3kiGsJc9Q-jktds06nvUMn1I-qxmLI7flw0khko6LCi6nTrBrPQ_XlQLYGS0C6-wkyCn0qqB6zsvX8kEElE7bEka0RkWzBVIWsVKkllajBNEeUVvAmy4KvmbFxXDlleNDXsJcETyo3-GmpaevrAr7ryIOOZuUQfkA8Hd-rb-lVkOkk2M-SChNlW8zNeyCksjp0LOF6r8SDf0fgnsKWsQ7qN2_Ltuvmd1X_HMIfdOmhi5Y6W6WnzWacO-jqbTb1k37bASbj2ViYB5-puTfEABzc12KN2mLRTA2TqhmYhy-9EPeDWdXsJwXKP1gtBrxXGFyEzuD4Y8lRYetY0RI5IJc8SoeUauJu75kNxUp-ffteIqP-uhG-E-BcFxIj_YfmI8Yui_XHU7OwDHTBdXk-ahBE7l-kvI-KN2hFcO1DIYK-c2no7fv1CXW8Lz-w7XWl54jn2MyKx4PnTUYT8langL6ZW3xux2wLrnbGwnIisWNWh0ih2ggKiiLCiOxJM9bwA-VbFSyihb3vWs-i4RAQtUwkvV6-8nyZv-SMcI9B7LuziJohnp_gIJZ8FIvtjoJHfGrZk"),
            P = Base64UrlEncoder.DecodeBytes("z3zVMYG1_TWwnNs99hyMJI9xs1zrwbV_STv9cqpaBxgPIHqpDyoG68a_VZF2RfuYLKT9ksEfLmgDeBPuM_vwBXSUApRqQo46WOpYUaDTcQJUJcspYQENQDkvtMGTuZQd6wrfGksDkUgnuZZ2CAKjN6vzGGlUkLJ9eEihbVTsCcYpp1sSK_J5HZCu0MZcnEU4oMbWKRHHApkQC8TJkutRw1XjY6s_ABj7M2W2D9CIO-CnUaXxmh21JNaJ2zP0nK1cfWgzvIikVoey7FKbGbOoIBnVf9AZUAVfclBLWI3wfUUVod_1lwLA07-7kvQ6SCEiHpsep3RwONcYJJH3vD3WlQ"),
            Q = Base64UrlEncoder.DecodeBytes("6l2EkIkJDFV_djyTFdpYbjrmg1lIRa1_oFF1925M0aDAEeJncRYkBOyupE9Ni6vHSTlRGN7I3Vs6V72Jnt_wpKqxz0qdhUseO89_JaqQE65euYq1QyeAjnHg64q4PcLYmBDub9Hn6y0xkTzkMCOUe5ZNGeosb5LW4qQVGyXnyeg9PmwFrBjQwr_Na6-1VrS2HCqzxvQWkOuJdjt3XB4ODXUnze9G-8nx8d2Cb9hC2qT4QxTPCq25i0n2DvIcRs_pD6RyNPg7EYOSH2Alc1vVzj7aYW9Plx69rIrtIFxH0ZwHsHua-AjDg7IZLK_Ghc3RV-9nTPEA2_6q_A_oOvfN9Q"),
        };

        /// <summary>
        /// Gets or sets the issuer to be stamped on the tokens created.
        /// </summary>
        public string Issuer { get; set; } = _defaultTestIssuer;

        /// <summary>
        /// Gets or sets the Audience to be stamped on the tokens created.
        /// </summary>
        public string Audience { get; set; } = _defaultTestAudience;

        /// <summary>
        /// Gets or sets the SigningCredentials used to sign the tokens created.
        /// </summary>
        public SigningCredentials SigningCredentials { get; set; }

        #region Create Test Token Methods
        /// <summary>
        /// Creates a default valid test token based based on the class's <see cref="Issuer"/>, <see cref="Audience"/>
        /// and <see cref="SigningCredentials"/> values.
        /// </summary>
        /// <returns>A test JWS token which is valid according to the class configuration.</returns>
        public string CreateDefaultValidToken()
        {
            var tokenDescriptor = CreateTokenDescriptorWithInstanceOverrides();
            return CreateToken(tokenDescriptor);
        }

        /// <summary>
        /// Creates a test token with a signature that doesn't match the payload.
        /// </summary>
        /// <returns>A test JWS token with a signature which doesn't match the payload.</returns>
        public string CreateTokenWithInvalidSignature()
        {
            var tokenDescriptor = CreateTokenDescriptorWithInstanceOverrides();
            var token = CreateToken(tokenDescriptor);
            return token.Substring(0, token.LastIndexOf('.')) + ".InvalidSignature";
        }

        /// <summary>
        /// Creates a test JWS token without any signature.
        /// </summary>
        /// <returns>A test JWS token without any signature.</returns>
        public string CreateTokenWithNoSignature()
        {
            var tokenDescriptor = CreateTokenDescriptorWithInstanceOverrides();
            var token = CreateToken(tokenDescriptor);
            return token.Substring(0, token.LastIndexOf('.')) + ".";
        }

        /// <summary>
        /// Creates a test JWS token which is past its expiration.
        /// </summary>
        /// <returns>A test JWS token which is past its expiration.</returns>
        public string CreateExpiredToken()
        {
            var tokenDescriptor = CreateTokenDescriptorWithInstanceOverrides();
            tokenDescriptor.NotBefore = DateTime.UtcNow - TimeSpan.FromHours(8);
            tokenDescriptor.IssuedAt = DateTime.UtcNow - TimeSpan.FromHours(8);
            tokenDescriptor.Expires = DateTime.UtcNow - TimeSpan.FromHours(4);

            return CreateToken(tokenDescriptor);
        }

        /// <summary>
        /// Creates a test JWS token which is not yet valid.
        /// </summary>
        /// <returns>A test JWS token which is not yet valid.</returns>
        public string CreateNotYetValidToken()
        {
            var tokenDescriptor = CreateTokenDescriptorWithInstanceOverrides();
            tokenDescriptor.NotBefore = DateTime.UtcNow + TimeSpan.FromHours(4);
            tokenDescriptor.Expires = DateTime.UtcNow + TimeSpan.FromHours(8);

            return CreateToken(tokenDescriptor);
        }

        /// <summary>
        /// Creates a test JWS token which is issued in the future.
        /// </summary>
        /// <returns>A test JWS token which is issued in the future.</returns>
        public string CreateTokenWithFutureIssuedAt()
        {
            var tokenDescriptor = CreateTokenDescriptorWithInstanceOverrides();
            tokenDescriptor.IssuedAt = DateTime.UtcNow + TimeSpan.FromHours(8);

            return CreateToken(tokenDescriptor);
        }

        /// <summary>
        /// Creates a test JWS token with an audience which doens't match the configured instance value.
        /// </summary>
        /// <returns>A test JWS token with an audience which doens't match the configured instance value.</returns>
        public string CreateTokenWithBadAudience()
        {
            var tokenDescriptor = CreateTokenDescriptorWithInstanceOverrides();
            tokenDescriptor.Audience = Guid.NewGuid().ToString();

            return CreateToken(tokenDescriptor);
        }

        /// <summary>
        /// Creates a test JWS token with an issuer which doens't match the configured instance value.
        /// </summary>
        /// <returns>A test JWS token with an issuer which doens't match the configured instance value.</returns>
        public string CreateTokenWithBadIssuer()
        {
            var tokenDescriptor = CreateTokenDescriptorWithInstanceOverrides();
            tokenDescriptor.Issuer = Guid.NewGuid().ToString();

            return CreateToken(tokenDescriptor);
        }

        /// <summary>
        /// Creates a test JWS token signed with a key that doens't match the configured instance value.
        /// </summary>
        /// <returns>A test JWS token signed with a key that doens't match the configured instance value.</returns>
        public string CreateTokenWithBadSignatureKey()
        {
            var tokenDescriptor = CreateTokenDescriptorWithInstanceOverrides();
            tokenDescriptor.SigningCredentials = new SigningCredentials(
                new RsaSecurityKey(_rsaParameters_4096),
                SecurityAlgorithms.RsaSha256,
                SecurityAlgorithms.Sha256);
            return CreateToken(tokenDescriptor);
        }

        /// <summary>
        /// Creates a test JWS token with a missing issuer (iss) claim.
        /// </summary>
        /// <returns>A test JWS token with a missing issuer (iss) claim.</returns>
        public string CreateTokenWithMissingIssuer()
        {
            var tokenDescriptor = CreateTokenDescriptorWithInstanceOverrides();

            // Remove the default value and null out any class specific override.
            tokenDescriptor.Subject.RemoveClaim(
                tokenDescriptor.Subject.FindFirst(JwtRegisteredClaimNames.Iss));
            tokenDescriptor.Issuer = null;
            return CreateToken(tokenDescriptor);
        }

        /// <summary>
        /// Creates a test JWS token with a missing audience (aud) claim.
        /// </summary>
        /// <returns>A test JWS token with a missing audience (aud) claim.</returns>
        public string CreateTokenWithMissingAudience()
        {
            var tokenDescriptor = CreateTokenDescriptorWithInstanceOverrides();

            // Remove the default value and null out any class specific override.
            tokenDescriptor.Subject.RemoveClaim(
                tokenDescriptor.Subject.FindFirst(JwtRegisteredClaimNames.Aud));
            tokenDescriptor.Audience = null;
            return CreateToken(tokenDescriptor);
        }

        /// <summary>
        /// Creates a test JWS token with a missing IssuedAt (iat) claim.
        /// </summary>
        /// <returns>A test JWS token with a missing IssuedAt (iat) claim.</returns>
        public string CreateTokenWithMissingIssuedAt()
        {
            var tokenDescriptor = CreateTokenDescriptorWithInstanceOverrides();

            // Remove the default value.
            tokenDescriptor.Subject.RemoveClaim(
                tokenDescriptor.Subject.FindFirst(JwtRegisteredClaimNames.Iat));
            return CreateToken(tokenDescriptor);
        }

        /// <summary>
        /// Creates a test JWS token with a missing NotBefore (nbf) claim.
        /// </summary>
        /// <returns>A test JWS token with a missing NotBefore (nbf) claim.</returns>
        public string CreateTokenWithMissingNotBefore()
        {
            var tokenDescriptor = CreateTokenDescriptorWithInstanceOverrides();

            // Remove the default value.
            tokenDescriptor.Subject.RemoveClaim(
                tokenDescriptor.Subject.FindFirst(JwtRegisteredClaimNames.Nbf));
            return CreateToken(tokenDescriptor);
        }

        /// <summary>
        /// Creates a test JWS token with a missing Expiration Time (exp) claim.
        /// </summary>
        /// <returns>A test JWS token with a missing Expiration Time (exp) claim.</returns>
        public string CreateTokenWithMissingExpires()
        {
            var tokenClaims = CreateClaimsSetWithInstanceOverrides();
            tokenClaims.Remove(JwtRegisteredClaimNames.Exp);

            return CreateToken(tokenClaims);
        }

        /// <summary>
        /// Creates a test JWS token without a signing key (i.e. alg=none, no signature).
        /// </summary>
        /// <returns>A test JWS token without a signing key (i.e. alg=none, no signature).</returns>
        public string CreateTokenWithMissingKey()
        {
            var tokenDescriptor = CreateTokenDescriptorWithInstanceOverrides();
            tokenDescriptor.SigningCredentials = null;
            return CreateToken(tokenDescriptor);
        }
        #endregion

        /// <summary>
        /// Creates a default set of claims based on the instance values.
        /// </summary>
        /// <returns>A <see cref="Dictionary{string, object}"/> representing the claims of a token to create.</returns>
        public Dictionary<string, object> CreateClaimsSetWithInstanceOverrides()
        {
            var claims = new Dictionary<string, object>();
            claims.Add(JwtRegisteredClaimNames.Iss, Issuer);
            claims.Add(JwtRegisteredClaimNames.Aud, Audience);
            claims.Add(JwtRegisteredClaimNames.Exp, EpochTime.GetIntDate(DateTime.UtcNow.AddDays(1)));
            claims.Add(JwtRegisteredClaimNames.Iat, EpochTime.GetIntDate(DateTime.UtcNow.AddDays(-1)));
            claims.Add(JwtRegisteredClaimNames.Nbf, EpochTime.GetIntDate(DateTime.UtcNow.AddDays(-1)));
            claims.Add(JwtRegisteredClaimNames.Email, "Alice@contoso.com");
            claims.Add(JwtRegisteredClaimNames.GivenName, "Alice");

            return claims;
        }

        /// <summary>
        /// Creates a default <see cref="SecurityTokenDescriptor"/> based on the instance values.
        /// </summary>
        /// <returns>A <see cref="SecurityTokenDescriptor"/> representing the token to create.</returns>
        public SecurityTokenDescriptor CreateTokenDescriptorWithInstanceOverrides()
        {
            var securityTokenDescriptor = new SecurityTokenDescriptor()
            {
                Subject = new ClaimsIdentity(_payloadClaims),
            };

            if (!string.IsNullOrEmpty(Issuer))
            {
                securityTokenDescriptor.Issuer = Issuer;
            }

            if (!string.IsNullOrEmpty(Audience))
            {
                securityTokenDescriptor.Audience = Audience;
            }

            if (SigningCredentials != null)
            {
                securityTokenDescriptor.SigningCredentials = SigningCredentials;
            }

            return securityTokenDescriptor;
        }

        /// <summary>
        /// Creates a token based on the passed <see cref="SecurityTokenDescriptor"/>.
        /// </summary>
        /// <param name="securityTokenDescriptor">The <see cref="SecurityTokenDescriptor"/> which describes the token to create.</param>
        /// <returns>A JWS token described by the passed <see cref="SecurityTokenDescriptor"/>.</returns>
        public static string CreateToken(SecurityTokenDescriptor securityTokenDescriptor)
        {
            var tokenHandler = new JsonWebTokenHandler()
            {
                SetDefaultTimesOnTokenCreation = false
            };

            return tokenHandler.CreateToken(securityTokenDescriptor);
        }

        /// <summary>
        /// Creates a token based on the passed <see cref="Dictionary{string, object}"/>.
        /// </summary>
        /// <param name="securityTokenDescriptor">
        /// The <see cref="Dictionary{string, object}"/> of claims which describe the token to create.
        /// </param>
        /// <returns>A JWS token described by the passed <see cref="Dictionary{string, object}"/>.</returns>
        public string CreateToken(Dictionary<string, object> claims)
        {
            var tokenHandler = new JsonWebTokenHandler()
            {
                SetDefaultTimesOnTokenCreation = false
            };

            return tokenHandler.CreateToken(CreateJsonPayload(claims), SigningCredentials);
        }

        /// <summary>
        /// Creates a JSON payload based on the passed <see cref="Dictionary{string, object}"/> of claims.
        /// </summary>
        /// <param name="claims">
        /// The <see cref="Dictionary{string, object}"/> of claims which describe the payload to create.</param>
        /// <returns>A JSON payload based on the passed <paramref name="claims"/>.</returns>
        public static string CreateJsonPayload(IDictionary<string, object> claims)
        {
            if (claims == null)
                throw new ArgumentNullException(nameof(claims));

            var jobj = new Microsoft.IdentityModel.Json.Linq.JObject();
            foreach (var claim in claims)
                jobj.Add(claim.Key, JToken.FromObject(claim.Value));

            return jobj.ToString(Formatting.None);
        }
    }
}
