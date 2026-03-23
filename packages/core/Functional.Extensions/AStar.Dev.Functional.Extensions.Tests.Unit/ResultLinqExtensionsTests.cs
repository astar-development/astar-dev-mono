// namespace AStar.Dev.Functional.Extensions.Tests.Unit;
//
// public class ResultLinqExtensionsTests
// {
//     private static Result<int, string>.Ok Ok(int value) => new(value);
//
//     private static Result<int, string>.Error Err(string error) => new(error);
//
//     [Fact]
//     public void Select_MapsSuccess()
//     {
//         var result = Ok(10).Select(x => x + 5);
//
//         var output = result.Match(
//                                   success => success,
//                                   error => -1);
//
//         Assert.Equal(15, output);
//     }
//
//     [Fact]
//     public void Select_PreservesError()
//     {
//         var result = Err("fail").Select(x => x + 5);
//
//         var output = result.Match(
//                                   success => success,
//                                   error => -1);
//
//         Assert.Equal(-1, output);
//     }
//
//     [Fact]
//     public void SelectMany_BindsSuccess()
//     {
//         Result<int, string> Double(int x)
//         {
//             return Ok(x * 2);
//         }
//
//         var result = Ok(3).SelectMany(Double);
//
//         var output = result.Match(
//                                   success => success,
//                                   error => -1);
//
//         Assert.Equal(6, output);
//     }
//
//     [Fact]
//     public void SelectMany_ShortCircuitsOnError()
//     {
//         Result<int, string> Double(int x)
//         {
//             return Ok(x * 2);
//         }
//
//         var result = Err("fail").SelectMany(Double);
//
//         var output = result.Match(
//                                   success => success,
//                                   error => -1);
//
//         Assert.Equal(-1, output);
//     }
//
//     [Fact]
//     public void SelectMany_WithProjection_ComposesCorrectly()
//     {
//         var result = Ok(2).SelectMany(
//                                       a => Ok(3),
//                                       (a, b) => a + b);
//
//         var output = result.Match(
//                                   success => success,
//                                   error => -1);
//
//         Assert.Equal(5, output);
//     }
//
//     [Fact]
//     public void SelectMany_WithProjection_PropagatesOuterError()
//     {
//         var result = Err("outer").SelectMany(
//                                              a => Ok(3),
//                                              (a, b) => a + b);
//
//         var output = result.Match(
//                                   success => success,
//                                   error => -1);
//
//         Assert.Equal(-1, output);
//     }
//
//     [Fact]
//     public void SelectMany_WithProjection_PropagatesInnerError()
//     {
//         var result = Ok(2).SelectMany(
//                                       a => Err("inner"),
//                                       (a, b) => a + b);
//
//         var output = result.Match(
//                                   success => success,
//                                   error => -1);
//
//         Assert.Equal(-1, output);
//     }
// }


